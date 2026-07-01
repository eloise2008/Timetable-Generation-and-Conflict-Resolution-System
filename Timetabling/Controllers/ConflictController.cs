using System;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;
using Timetabling.Models;
using Timetabling.Services;

namespace Timetabling.Controllers
{
    public class ConflictController : BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        // GET /Conflict
        public ActionResult Index()
        {
            ViewBag.Title = "Conflict Resolution";
            return View(new UploadViewModel
            {
                AcademicYear = DateTime.Now.Year + "/" + (DateTime.Now.Year + 1),
                Semester = "Alpha"
            });
        }

        // POST /Conflict/Upload
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Upload(UploadViewModel model, HttpPostedFileBase timetableFile)
        {
            ViewBag.Title = "Conflict Resolution";

            if (timetableFile == null || timetableFile.ContentLength == 0)
            {
                TempData["Error"] = "Please select an Excel file to upload.";
                return RedirectToAction("Index");
            }

            var ext = System.IO.Path.GetExtension(timetableFile.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["Error"] = "Only Excel files (.xlsx, .xls) are supported.";
                return RedirectToAction("Index");
            }

            try
            {
                var courses = _db.GetCoursesBySemester(model.Semester);
                var rooms = _db.GetAllRooms();
                var slots = _db.GetAllTimeSlots();
                var resolver = new ConflictResolver(courses, rooms, slots);

                var report = resolver.ParseAndDetect(
                    timetableFile, model.AcademicYear, model.Semester);

                Session["ConflictReport"] = report;
                Session["UploadModel"] = model;

                return View("Report", report);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to parse file: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST /Conflict/Resolve
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Resolve()
        {
            var report = Session["ConflictReport"] as ConflictReport;
            var model = Session["UploadModel"] as UploadViewModel;

            if (report == null)
            {
                TempData["Error"] = "Session expired. Please upload the file again.";
                return RedirectToAction("Index");
            }

            try
            {
                var courses = _db.GetCoursesBySemester(model.Semester);
                var rooms = _db.GetAllRooms();
                var slots = _db.GetAllTimeSlots();
                var resolver = new ConflictResolver(courses, rooms, slots);

                var resolved = resolver.ResolveConflicts(report);

                if (resolved == null)
                {
                    TempData["Error"] = "Could not resolve all conflicts automatically. " +
                        "Try adding more rooms or time slots.";
                    return View("Report", report);
                }

                // Save as new session
                var session = new TimetableSession
                {
                    SessionName = model.AcademicYear + " " + model.Semester +
                                   " Semester (Conflict Resolved)",
                    AcademicYear = model.AcademicYear,
                    Semester = model.Semester,
                    GeneratedBy = Session["FullName"]?.ToString() ?? "Admin",
                    TotalCourses = resolved.Count
                };

                int sessionId = _db.CreateSession(session);
                _db.SaveTimetableSession(resolved, sessionId);

                Session["ConflictReport"] = null;
                Session["UploadModel"] = null;

                TempData["Success"] = "Conflicts resolved! " + resolved.Count +
                    " courses scheduled in new session.";
                return RedirectToAction("ViewSession", "Timetable",
                    new { id = sessionId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Resolution failed: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}