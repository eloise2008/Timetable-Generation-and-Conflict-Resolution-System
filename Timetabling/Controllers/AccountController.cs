using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Timetabling.Data;
using Timetabling.Helpers;
using Timetabling.Models;

namespace Timetabling.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        // GET /Account/Login
        public ActionResult Login()
        {
            // Already logged in — redirect to dashboard
            if (Session["UserId"] != null)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        // POST /Account/Login
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _db.GetUserByUsername(model.Username);

            if (user == null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
            {
                model.ErrorMessage = "Invalid username or password.";
                return View(model);
            }

            // Store user info in session
            Session["UserId"] = user.UserId;
            Session["Username"] = user.Username;
            Session["FullName"] = user.FullName;
            Session["Role"] = user.Role;

            return RedirectToAction("Index", "Home");
        }

        // POST /Account/Logout
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }
        // Temporary — visit /Account/ResetAdminPassword once then remove it
        public ActionResult ResetAdminPassword()
        {
            var hash = PasswordHasher.Hash("Admin@123");
            var user = _db.GetUserByUsername("admin");
            if (user != null)
                _db.UpdatePassword(user.UserId, hash);
            return Content("Password reset to Admin@123. Hash: " + hash);
        }
    }
}