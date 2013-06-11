using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;
using Iroha.WebPages.Models;

namespace Iroha.WebPages.Services
{
    public class ContentManager
    {
        //private IDictionary<String, Containable> _containableCache = new Dictionary<string, Containable>();

        public String VersionsRootDirectory { get; private set; }
        public String SiteRootDirectory { get; private set; }
        public WindowsPrincipal User { get; set; }

        public ContentManager(String siteRootDirectory, String versionsRootDirectory, WindowsPrincipal currentUser)
        {
            SiteRootDirectory = siteRootDirectory;
            VersionsRootDirectory = versionsRootDirectory;
            User = currentUser;
        }

        public void UpdateContentPage(ContentPage contentPage)
        {
            // current -> version
            var currentContentPage = GetContentPage(contentPage.Path, contentPage.Parent);
            CreateNewContentPageVersion(currentContentPage);

            // save
            contentPage.SaveBody();
            SaveContentPageMetadata(contentPage, contentPage.Metadata);

            // rebuild _LocalNavItems
            UpdateLocalNavItems(contentPage.Parent, true);
        }

        public void UpdateLocalNavItems(Container container, Boolean updateParent)
        {
            var localNavItemsString =
                "@* Automatic Generated *@@RenderPage(\"~/_Shared/Partials/_LocalNavItems.cshtml\", new { ";

            localNavItemsString += "Contents = new Dictionary<String, String>{";
            container = GetContainer(container.Path, null); // reload
            localNavItemsString += String.Join(",",
                Enumerable.Union(
                    container.Contents
                        .OfType<ContentPage>()
                        .Where(x => !x.Alias.StartsWith("_") && (String.Compare(x.Alias, "Default", true) != 0))
                        .Select(x =>
                                String.Format("{{ \"{0}\", \"~{1}\" }}",
                                              Utility.EscapeCSharpString(x.Title), Utility.EscapeCSharpString(Regex.Replace(x.Path, "/Default$", "/", RegexOptions.IgnoreCase)))
                        ),
                    container.Contents
                        .OfType<Container>()
                        .Where(x => !x.Alias.StartsWith("_") && x.Contents.OfType<ContentPage>().Any(x2 => (String.Compare(x2.Alias, "Default", true) == 0)))
                        .Select(x =>
                                String.Format("{{ \"{0}\", \"~{1}\" }}",
                                              Utility.EscapeCSharpString(x.Title), Utility.EscapeCSharpString(Regex.Replace(x.Path, "/Default$", "/", RegexOptions.IgnoreCase)))
                        )
                    )
                );
            if (container.Parent != null)
            {
                localNavItemsString += String.Format("}}, Parent = new {{ Title = \"{0}\", Path = \"{1}\" }}", Utility.EscapeCSharpString(container.Parent.Title), Utility.EscapeCSharpString(container.Parent.Path));
            }
            else
            {
                localNavItemsString += "}, Parent = new { Title = \"\", Path = \"\" }";
            }
            localNavItemsString += String.Format(", Current = new {{ Title = \"{0}\", Path = \"{1}\" }}", Utility.EscapeCSharpString(container.Title), Utility.EscapeCSharpString(container.Path));
            localNavItemsString += "})";

            File.WriteAllText(Path.Combine(container.PhysicalPath, "_LocalNavItems.cshtml"), localNavItemsString, new UTF8Encoding(true));

            if (updateParent && container.Parent != null)
            {
                UpdateLocalNavItems(container.Parent, false);
            }
            if (updateParent && container.Contents.OfType<Container>().Any())
            {
                foreach (var c in container.Contents.OfType<Container>())
                {
                    UpdateLocalNavItems(c, false);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Containable GetContainable(String path)
        {
            var dir = GetContainer(path, null);
            if (dir != null) return dir;
            var contentPage = GetContentPage(path, null);
            if (contentPage != null) return contentPage;

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parentContainer"></param>
        /// <returns></returns>
        public ContentPage GetContentPage(String path, Container parentContainer)
        {
            path = path.Trim('/');

            var contentPageFullPath = Path.Combine(SiteRootDirectory, path + ".cshtml");
            if (File.Exists(contentPageFullPath))
            {
                var canRead = false;
                var canWrite = false;

                CheckAccessControl(
                    Directory.GetAccessControl(contentPageFullPath).GetAccessRules(true, true, typeof(SecurityIdentifier)),
                    out canRead, out canWrite);

                var contentPage = new ContentPage()
                           {
                               PhysicalPath = contentPageFullPath,
                               Alias = Path.GetFileName(path),
                               CanRead = canRead,
                               CanWrite = canWrite,
                               Parent = parentContainer
                           };

                if (contentPage.Parent == null)
                {
                    if (path.LastIndexOf('/') > 0)
                    {
                        // 親がルート以外
                        contentPage.Parent = GetContainer(path.Substring(0, path.LastIndexOf('/')), null);
                    }
                    else if (!String.IsNullOrWhiteSpace(path))
                    {
                        // 親がルート
                        contentPage.Parent = GetContainer("", null);
                    }
                }

                // Metadata
                var metadata = ReadContentPageMetadata(contentPage);
                contentPage.Metadata = metadata;

                return contentPage;
            }
            return null;
        }

        public ContentPage CreateContentPage(Container parentContainer, String pageAlias)
        {
            if (parentContainer == null) throw new ArgumentNullException("parentContainer");
            if (String.IsNullOrWhiteSpace(pageAlias)) throw new ArgumentException("pageAlias");

            if (Regex.IsMatch(pageAlias, "[*?|:<>\"/\\\\]|[\\p{C}-[ ]]"))
                throw new ArgumentException("ページ名には * ? | \" < > : / \\ および制御文字を含めることはできません");
            if (Regex.IsMatch(pageAlias, "^[.]"))
                throw new ArgumentException("ページ名をドットではじめることはできません。");

            if (!Directory.Exists(parentContainer.PhysicalPath))
                throw new DirectoryNotFoundException(String.Format("パス {0} が見つかりません", parentContainer.PhysicalPath));

            var newContentPagePath = Path.Combine(parentContainer.PhysicalPath, pageAlias) + ".cshtml";
            if (File.Exists(newContentPagePath) || Directory.Exists(newContentPagePath))
                throw new ArgumentException("指定された名前のページまたはコンテナはすでに存在しています");

            File.WriteAllText(newContentPagePath, @"@* Using:Iroha.WebPages *@<!-- Using:Iroha.ComponentEditor -->");

            if (parentContainer.CanWrite)
            {
                var contentPage = GetContentPage(parentContainer.Path + "/" + pageAlias, parentContainer);
                return contentPage;
            }

            throw new UnauthorizedAccessException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentContainer"></param>
        /// <param name="newContainerName"></param>
        /// <returns></returns>
        public Container CreateContainer(Containable parentContainer, String newContainerName)
        {
            if (newContainerName == null) throw new ArgumentNullException("newContainerName");
            if (String.IsNullOrWhiteSpace(newContainerName)) throw new ArgumentNullException("containerName");

            if (Regex.IsMatch(newContainerName, "[*?|:<>\"/\\\\]|[\\p{C}-[ ]]"))
                throw new ArgumentException("コンテナ名には * ? | \" < > : / \\ および制御文字を含めることはできません");
            if (Regex.IsMatch(newContainerName, "^[_.]"))
                throw new ArgumentException("コンテナ名をドットまたは_ではじめることはできません。");

            var newContainerPath = Path.Combine(parentContainer.PhysicalPath, newContainerName);
            if (Directory.Exists(newContainerPath) || Directory.Exists(newContainerPath))
                throw new ArgumentException("指定された名前のページまたはコンテナはすでに存在しています");

            if (parentContainer.CanWrite)
            {
                Directory.CreateDirectory(newContainerPath);
                return GetContainer(parentContainer.Path + "/" + newContainerName, null);
            }

            throw new UnauthorizedAccessException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="containable"></param>
        /// <returns></returns>
        public void Delete(Containable containable)
        {
            if (containable is Container)
            {
                // TODO: ContentPageと同様に子供ページをバージョンに追加する
                Directory.Delete(containable.PhysicalPath, true);
            }
            else
            {
                // current -> version
                var currentContentPage = GetContentPage(containable.Path, containable.Parent);
                CreateNewContentPageVersion(currentContentPage);

                File.Delete(containable.PhysicalPath);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parentContainer"></param>
        /// <returns></returns>
        public Container GetContainer(String path, Container parentContainer)
        {
            path = path.Trim('/');

            var dirFullPath = Path.Combine(SiteRootDirectory, path);
            if (Directory.Exists(dirFullPath))
            {
                var canRead = false;
                var canWrite = false;

                CheckAccessControl(
                    Directory.GetAccessControl(dirFullPath).GetAccessRules(true, true, typeof (SecurityIdentifier)),
                    out canRead, out canWrite);

                var container = new Container
                           {
                               Parent = parentContainer,
                               Alias = Path.GetFileName(path),
                               PhysicalPath = dirFullPath,
                               CanRead = canRead,
                               CanWrite = canWrite,
                           };

                // 子供のコンテナ
                container.Contents = GetContainers(dirFullPath, container);

                // ページ
                var contentPages = Directory.GetFiles(dirFullPath, "*.cshtml")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Select(x => path + "/" + x)
                    .Select(x => GetContentPage(x, container));

                container.Contents = container.Contents.Union(contentPages);

                if (container.Parent == null)
                {
                    if (path.LastIndexOf('/') > 0)
                    {
                        // 親がルート以外
                        container.Parent = GetContainer(path.Substring(0, path.LastIndexOf('/')), null);
                    }
                    else if (!String.IsNullOrWhiteSpace(path))
                    {
                        // 親がルート
                        container.Parent = GetContainer("", null);
                    }
                }
                return container;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parentContainer"></param>
        /// <returns></returns>
        public IEnumerable<Container> GetContainers(String path, Container parentContainer)
        {
            path = path.Trim('/');

            var fullPath = Path.Combine(SiteRootDirectory, path);

            if (!Directory.Exists(fullPath))
                return Enumerable.Empty<Container>();

            return Directory.GetDirectories(fullPath)
                            .Where(x => !Regex.IsMatch(Path.GetFileName(x), "^([._]|App_)|^bin$"))
                            .Select(x => GetContainer(x, parentContainer))
                            .Where(x => x != null);
        }

        private void CheckAccessControl(AuthorizationRuleCollection authorizationRules, out Boolean canRead, out Boolean canWrite)
        {
            var rules = authorizationRules
                            .OfType<FileSystemAccessRule>()
                            .Where(x => User.IsInRole(x.IdentityReference as SecurityIdentifier) || x.IdentityReference == ((WindowsIdentity)User.Identity).User)
                            .ToList();
            canRead = true;
            canWrite = true;

            canRead &= rules.Any(x => x.AccessControlType == AccessControlType.Allow &&
                                        x.FileSystemRights.HasFlag(FileSystemRights.Read));
            canWrite &= rules.Any(x => x.AccessControlType == AccessControlType.Allow &&
                                        x.FileSystemRights.HasFlag(FileSystemRights.Write));
            canRead &= !rules.Any(x => x.AccessControlType == AccessControlType.Deny &&
                                        x.FileSystemRights.HasFlag(FileSystemRights.Read));
            canWrite &= !rules.Any(x => x.AccessControlType == AccessControlType.Deny &&
                                        x.FileSystemRights.HasFlag(FileSystemRights.Write));

        }

        public ContentPageMetadata ReadContentPageMetadata(ContentPage contentPage)
        {
            var metadataDir = Path.Combine(contentPage.Parent.PhysicalPath, "_IrohaMetadata");
            var metadataPath = Path.Combine(metadataDir, contentPage.Alias + ".xml");
            if (File.Exists(metadataPath))
            {
                return ContentPageMetadata.LoadFromFile(metadataPath);
            }

            return new ContentPageMetadata()
                       {
                           Title = contentPage.Alias,
                           CreatedAt = File.GetCreationTime(contentPage.PhysicalPath),
                           ModifiedAt = File.GetLastWriteTime(contentPage.PhysicalPath),
                           CreatedBy = "",
                           ModifiedBy = ""
                       };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentPage"></param>
        /// <param name="metadata"></param>
        public void SaveContentPageMetadata(ContentPage contentPage, ContentPageMetadata metadata)
        {
            var metadataDir = Path.Combine(contentPage.Parent.PhysicalPath, "_IrohaMetadata");
            var metadataPath = Path.Combine(metadataDir, contentPage.Alias + ".xml");
            if (!Directory.Exists(metadataDir))
            {
                Directory.CreateDirectory(metadataDir);
            }
            metadata.Save(metadataPath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentPage"></param>
        public void CreateNewContentPageVersion(ContentPage contentPage)
        {
            var versionsDir = Path.Combine(VersionsRootDirectory, contentPage.Path.Trim('/', '\\'));
            if (!Directory.Exists(versionsDir))
            {
                Directory.CreateDirectory(versionsDir);
            }

            contentPage.LoadBody();
            var version = new ContentPageVersion() {Metadata = contentPage.Metadata, Content = contentPage.Body};
            version.Save(Path.Combine(versionsDir, contentPage.ModifiedAt.Ticks + ".xml"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentPage"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public ContentPageVersion GetVersion(ContentPage contentPage, Int64 version)
        {
            var versionsDir = Path.Combine(VersionsRootDirectory, contentPage.Path.Trim('/', '\\'));
            var versionFilePath = Path.Combine(versionsDir, version + ".xml");
            if (!File.Exists(versionFilePath))
                return null;

            return ContentPageVersion.LoadFromFile(versionFilePath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentPage"></param>
        /// <returns></returns>
        public IEnumerable<DateTime> GetVersions(ContentPage contentPage)
        {
            var versionsDir = Path.Combine(VersionsRootDirectory, contentPage.Path.Trim('/', '\\'));
            if (!Directory.Exists(versionsDir))
            {
                return Enumerable.Empty<DateTime>();
            }

            return Directory.GetFiles(versionsDir, "*.xml")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(x => Regex.IsMatch(x, "^[0-9]+$"))
                .Select(x => DateTime.FromBinary(Int64.Parse(x)))
                .OrderByDescending(x => x);
        }
    }
}