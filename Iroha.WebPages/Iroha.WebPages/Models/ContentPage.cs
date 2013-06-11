using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Iroha.WebPages.Models
{
    public class ContentPage : Containable
    {
        public override String Title
        {
            get { return Metadata.Title; }
            set { Metadata.Title = value; }
        }
        public DateTime CreatedAt { get { return Metadata.CreatedAt; } }
        public DateTime ModifiedAt { get { return Metadata.ModifiedAt; } }
        public ContentPageMetadata Metadata { get; set; }

        public Boolean IsRawContent { get; set; }

        public String Body { get; set; }

        public void LoadBody()
        {
            var bodyText = File.ReadAllText(PhysicalPath, Encoding.UTF8);
            var isRawContent = !Regex.IsMatch(bodyText, @"\s*@\*\s*Using:Iroha.WebPages\s*\*@\s*", RegexOptions.IgnoreCase);

            if (isRawContent)
            {
                Body = bodyText;
                IsRawContent = true;
            }
            else
            {
                Body = Regex.Replace(bodyText, @"\s*@{\s*/\*\s*Generated:Iroha.WebPages\s*\*/\s*[\s\S]*?\s*/\*\s*Generated:Iroha.WebPages\s*\*/\s*}\s*", "", RegexOptions.IgnoreCase);
                Body = Regex.Replace(Body, @"\s*@\*\s*Using:Iroha.WebPages\s*\*@\s*", "", RegexOptions.IgnoreCase);
                Body = Body.Replace("@@", "@");
                IsRawContent = false;
            }
        }

        public void SaveBody()
        {
            var bodyText = Body;
            if (!IsRawContent)
            {
                var header = String.Format(@"@* Using:Iroha.WebPages *@
@{{/*Generated:Iroha.WebPages*/
Page.Title = ""{0}"";
/*Generated:Iroha.WebPages*/}}
", Title.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", ""));

                bodyText = header + bodyText.Replace("@", "@@");
            }

            File.WriteAllText(PhysicalPath, bodyText, new UTF8Encoding(true));
        }
    }
}