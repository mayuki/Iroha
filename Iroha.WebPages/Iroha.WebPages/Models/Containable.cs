using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Iroha.WebPages.Models
{
    public class Containable
    {
        public virtual String Title { get; set; }
        public String Alias { get; set; }

        public virtual Container Parent { get; set; }

        public virtual String Path
        {
            get { return ((Parent != null) ? Parent.Path : "") + Alias; }
        }
        public String PhysicalPath { get; set; }

        public Boolean CanRead { get; set; }
        public Boolean CanWrite { get; set; }
    }
}