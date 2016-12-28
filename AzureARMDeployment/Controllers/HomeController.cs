using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AzureARMDeployment.Models;

namespace AzureARMDeployment.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View(new DeployParamModel());
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}