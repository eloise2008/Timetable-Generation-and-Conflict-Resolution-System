using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Timetabling.Controllers
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // If not logged in, redirect to login page
            if (Session["UserId"] == null)
            {
                filterContext.Result = new RedirectResult("/Account/Login");
                return;
            }

            // Pass user info to all views
            ViewBag.FullName = Session["FullName"];
            ViewBag.Username = Session["Username"];

            base.OnActionExecuting(filterContext);
        }
    }
}