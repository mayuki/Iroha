using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Iroha.WebPages.Models;
using Iroha.WebPages.Services;
using Iroha.WebPages.ViewModels.Pages;

namespace Iroha.WebPages.Controllers
{
    public class PagesController : Controller
    {
        private Container _rootContainer;
        private Containable _targetContainable;
        private ContentManager _contentManager;

        public PagesController()
        {
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var siteRootDir = ConfigurationManager.AppSettings["Iroha.WebPages.SiteRootDirectory"];
            if (siteRootDir.StartsWith("~/"))
                siteRootDir = Server.MapPath(siteRootDir);
            var versionsDir = ConfigurationManager.AppSettings["Iroha.WebPages.VersionsDirectory"];
            if (versionsDir.StartsWith("~/"))
                versionsDir = Server.MapPath(versionsDir);

            _contentManager = new ContentManager(siteRootDir, versionsDir,new WindowsPrincipal(User.Identity as WindowsIdentity));

            _rootContainer = _contentManager.GetContainer("", null);
            _targetContainable = _contentManager.GetContainable(filterContext.RouteData.Values.ContainsKey("pagePath") ? filterContext.RouteData.Values["pagePath"].ToString() : "");

            base.OnActionExecuting(filterContext);
        }

        public ActionResult List(String pagePath)
        {
            pagePath = pagePath ?? "";

            var container = _contentManager.GetContainer(pagePath, null);

            if (container == null)
                return HttpNotFound();
            if (!container.CanRead)
                return new HttpStatusCodeResult(403, "Forbidden");

            return View(new ListViewModel { RootContainer = _rootContainer, Container = container });
        }

        [HttpPost]
        public ActionResult Delete(String pagePath)
        {
            var target = _contentManager.GetContainable(pagePath ?? "");
            if (target == null)
                return HttpNotFound();

            if (!target.CanWrite)
                return new HttpStatusCodeResult(403, "Forbidden");

            if (target is Container)
            {
                // Root コンテナは消せない
                if (target.Parent == null)
                    return new HttpStatusCodeResult(403, "Forbidden");
            }

            _contentManager.Delete(target);

            return RedirectToAction("List", new { PagePath = target.Parent.Path });
        }


        [HttpGet]
        public ActionResult CreateContainer(String pagePath)
        {
            var rootContainer = _contentManager.GetContainer("", null);
            var container = _contentManager.GetContainer(pagePath ?? "", null);
            if (container == null)
                return HttpNotFound();

            if (!container.CanWrite)
                return new HttpStatusCodeResult(403, "Forbidden");

            return View(new CreateContainerViewModel() { RootContainer = rootContainer, Container = container, InputModel = new CreateContainerInputModel() });
        }

        [HttpPost]
        [ActionName("CreateContainer")]
        public ActionResult CreateContainerPost(CreateContainerInputModel inputModel)
        {
            var rootContainer = _contentManager.GetContainer("", null);
            var container = _contentManager.GetContainer(inputModel.PagePath ?? "", null);
            if (container == null)
                return HttpNotFound();
            if (!container.CanWrite)
                return new HttpStatusCodeResult(403, "Forbidden");

            if (ModelState.IsValid)
            {
                if (container.Contents.Any(x => String.Compare(x.Alias, inputModel.ContainerName, false) == 0))
                {
                    ModelState.AddModelError("ContainerName", "コンテナまたはページがすでに存在しています。");
                    return View("CreateContainer", new CreateContainerViewModel() { RootContainer = rootContainer, Container = container, InputModel = inputModel });
                }
            }
            else
            {
                return View("CreateContainer", new CreateContainerViewModel() { RootContainer = rootContainer, Container = container, InputModel = inputModel });
            }

            var newContainer = _contentManager.CreateContainer(container, inputModel.ContainerName);

            return RedirectToAction("List", new { pagePath = newContainer.Path });
        }
        [HttpGet]
        public ActionResult CreateContentPage(String pagePath)
        {
            var rootContainer = _contentManager.GetContainer("", null);
            var container = _contentManager.GetContainer(pagePath ?? "", null);
            if (container == null)
                return HttpNotFound();

            if (!container.CanWrite)
                return new HttpStatusCodeResult(403, "Forbidden");

            return View(new CreateContentPageViewModel() { RootContainer = rootContainer, Container = container, InputModel = new CreateContentPageInputModel() });
        }

        [HttpPost]
        [ActionName("CreateContentPage")]
        public ActionResult CreateContentPagePost(CreateContentPageInputModel inputModel)
        {
            var rootContainer = _contentManager.GetContainer("", null);
            var container = _contentManager.GetContainer(inputModel.PagePath ?? "", null);
            if (container == null)
                return HttpNotFound();

            if (!container.CanWrite)
                return new HttpStatusCodeResult(403, "Forbidden");

            if (ModelState.IsValid)
            {
                if (container.Contents.Any(x => String.Compare(x.Alias, inputModel.ContentName, false) == 0))
                {
                    ModelState.AddModelError("ContainerName", "コンテナまたはページがすでに存在しています。");
                    return View("CreateContentPage", new CreateContentPageViewModel() { RootContainer = rootContainer, Container = container, InputModel = inputModel });
                }
            }
            else
            {
                return View("CreateContentPage", new CreateContentPageViewModel() { RootContainer = rootContainer, Container = container, InputModel = inputModel });
            }

            var newContentPage = _contentManager.CreateContentPage(container, inputModel.ContentName);
            newContentPage.Metadata.CreatedAt = newContentPage.Metadata.ModifiedAt = DateTime.Now;
            newContentPage.Metadata.CreatedBy = newContentPage.Metadata.ModifiedBy = User.Identity.Name;
            _contentManager.SaveContentPageMetadata(newContentPage, newContentPage.Metadata);

            return RedirectToAction("Edit", new { pagePath = newContentPage.Path });
        }


        public ActionResult Edit(String pagePath, Int64? version)
        {
            var rootContainer = _contentManager.GetContainer("", null);
            var contentPage = _contentManager.GetContentPage(pagePath ?? "", null);
            if (contentPage == null)
                return HttpNotFound();

            if (!contentPage.CanWrite)
                return new HttpStatusCodeResult(403, "Forbidden");

            contentPage.LoadBody();

            ContentPageVersion contentPageVersion = null;
            if (version.HasValue)
            {
                contentPageVersion = _contentManager.GetVersion(contentPage, version.Value);
                if (contentPageVersion == null)
                    return HttpNotFound();

                contentPage.Body = contentPageVersion.Content;
                contentPage.Title = contentPageVersion.Metadata.Title;
            }

            return View(new EditViewModel()
                            {
                                Content = contentPage,
                                RootContainer = rootContainer,
                                Versions = _contentManager.GetVersions(contentPage),
                                Version = contentPageVersion,
                                InputModel = new EditInputModel()
                                                 {
                                                     Body = contentPage.Body,
                                                     ContentType = "application/x-razor",
                                                     Title = contentPage.Title
                                                 }
                            });
        }

        [HttpPost]
        [ActionName("Edit")]
        public ActionResult EditPost(String pagePath, EditInputModel inputModel)
        {
            var rootContainer = _contentManager.GetContainer("", null);
            var contentPage = _contentManager.GetContentPage(pagePath ?? "", null);
            if (contentPage == null)
                return HttpNotFound();

            contentPage.LoadBody();

            if (!contentPage.CanWrite || contentPage.IsRawContent)
                return new HttpStatusCodeResult(403, "Forbidden");

            if (ModelState.IsValid)
            {
                contentPage.Title = contentPage.Metadata.Title = (inputModel.Title ?? "");
                contentPage.Body = inputModel.Body ?? "";
                contentPage.Metadata.ModifiedAt = DateTime.Now;
                contentPage.Metadata.ModifiedBy = User.Identity.Name;
                _contentManager.UpdateContentPage(contentPage);

                TempData["Notice"] = "保存しました。";
                return RedirectToAction("Edit", new { pagePath = contentPage.Path });
            }

            return View(new EditViewModel() { RootContainer = rootContainer, Content = contentPage, InputModel = inputModel });
        }

        public JsonResult IsNewContainerAvailable(CreateContainerInputModel inputModel)
        {
            var container = _contentManager.GetContainer(inputModel.PagePath ?? "", null);
            if (container == null || !container.CanRead)
                return Json("コンテナが見つかりません", JsonRequestBehavior.AllowGet);
            if (!container.CanWrite)
                return Json("作成する権限がありません", JsonRequestBehavior.AllowGet);

            if (container.Contents.Any(x => String.Compare(x.Alias, inputModel.ContainerName, false) == 0))
            {
                ModelState.AddModelError("ContainerName", "コンテナまたはページがすでに存在しています。");
            }

            if (ModelState.IsValid)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(ModelState.Values.Where(x => x.Errors.Any()).First().Errors.First().ErrorMessage,
                            JsonRequestBehavior.AllowGet);
            }
        }
    }
}
