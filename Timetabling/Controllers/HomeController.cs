using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;

namespace Timetabling.Controllers
{
    public class HomeController : BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public ActionResult Index()
        {
            ViewBag.Title = "Dashboard";
            ViewBag.CourseCount = _db.Count("COURSE");
            ViewBag.LecturerCount = _db.Count("LECTURER");
            ViewBag.RoomCount = _db.Count("ROOM");
            ViewBag.SlotCount = _db.Count("TIMESLOT");
            ViewBag.DeptCount = _db.Count("DEPARTMENT");
            ViewBag.TTCount = _db.Count("TIMETABLE");
            return View();
        }
    }
}