using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Iroha.WebPages.Services;

namespace Iroha.WebPages.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("List", "Pages");
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
