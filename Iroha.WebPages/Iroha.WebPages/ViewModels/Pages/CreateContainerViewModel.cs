using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Iroha.WebPages.Models;

namespace Iroha.WebPages.ViewModels.Pages
{
    public class CreateContainerViewModel
    {
        public Container RootContainer { get; set; }
        public Container Container { get; set; }

        public CreateContainerInputModel InputModel { get; set; }
    }
}
