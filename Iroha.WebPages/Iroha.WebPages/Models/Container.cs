using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Iroha.WebPages.Models
{
    public class Container : Containable
    {
        public IEnumerable<Containable> Contents { get; set; }

        public override String Path { get { return base.Path + "/"; } }

        public override string ToString()
        {
            return String.Format("Container: Path={0}; Alias={1}", Path, Alias);
        }

        public Container()
        {
            Contents = new List<Containable>();
        }

        
        public override String Title
        {
            get
            {
                var defaultPage =
                    Contents.OfType<ContentPage>().Where(x => String.Compare(x.Alias, "Default", true) == 0).FirstOrDefault();
                
                return (defaultPage != null) ? defaultPage.Title : (Parent == null && String.IsNullOrWhiteSpace(Alias) ? "Root" : Alias);
            }
        }
    }
}