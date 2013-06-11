using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Iroha.WebPages.ViewModels.Pages
{
    public class EditInputModel
    {
        [StringLength(255)]
        public String Title { get; set; }
        public String Body { get; set; }
        public String ContentType { get; set; }
    }
}
