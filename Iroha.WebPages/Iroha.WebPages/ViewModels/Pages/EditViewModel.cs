using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iroha.WebPages.Models;

namespace Iroha.WebPages.ViewModels.Pages
{
    public class EditViewModel
    {
        public Container RootContainer { get; set; }
        public ContentPage Content { get; set; }
        public IEnumerable<DateTime> Versions { get; set; }
        public ContentPageVersion Version { get; set; }

        public EditInputModel InputModel { get; set; }
    }
}
