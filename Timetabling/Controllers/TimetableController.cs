using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;
using Timetabling.Services;
using Timetabling.Models;


namespace Timetabling.Controllers
{
    public class TimetableController : BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();
        private readonly Scheduler _sch = new Scheduler();

        // GET /Timetable — show latest session
        public ActionResult Index()
        {
            ViewBag.Title = "Timetable";
            ViewBag.Sessions = _db.GetAllSessions();
            try
            {
                var sessions = _db.GetAllSessions();
                if (sessions.Count > 0)
                    return View(_db.GetTimetableBySession(sessions[0].SessionId));
                return View(new List<Assignment>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<Assignment>());
            }
        }

        // GET /Timetable/ViewSession/5
        public ActionResult ViewSession(int id)
        {
            var session = _db.GetSessionById(id);
            if (session == null) return HttpNotFound();
            ViewBag.Title = session.SessionName;
            ViewBag.Session = session;
            ViewBag.Sessions = _db.GetAllSessions();
            return View("Index", _db.GetTimetableBySession(id));
        }

        // GET /Timetable/Generate
        public ActionResult Generate()
        {
            ViewBag.Title = "Generate Timetable";
            ViewBag.AlphaCount = _db.GetCoursesBySemester("Alpha").Count;
            ViewBag.OmegaCount = _db.GetCoursesBySemester("Omega").Count;
            ViewBag.RoomCount = _db.GetAllRooms().Count;
            ViewBag.SlotCount = _db.GetAllTimeSlots().Count;
            ViewBag.Capacity = _db.GetAllRooms().Count * _db.GetAllTimeSlots().Count;
            return View(new GenerateViewModel
            {
                AcademicYear = DateTime.Now.Year + "/" + (DateTime.Now.Year + 1),
                Semester = "Alpha",
                SessionName = ""
            });
        }

        // POST /Timetable/Generate
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Generate(GenerateViewModel model)
        {
            try
            {
                var courses = _db.GetCoursesBySemester(model.Semester);
                var rooms = _db.GetAllRooms();
                var slots = _db.GetAllTimeSlots();

                if (courses.Count == 0)
                {
                    TempData["Error"] = "No " + model.Semester +
                        " semester courses found. Please set the Semester field on your courses first.";
                    return RedirectToAction("Generate");
                }

                var result = _sch.GenerateTimetable(courses, rooms, slots);

                if (result == null)
                {
                    TempData["Error"] = "No valid timetable could be generated. " +
                        "Try adding more rooms or time slots.";
                    return RedirectToAction("Generate");
                }

                var session = new TimetableSession
                {
                    SessionName = !string.IsNullOrWhiteSpace(model.SessionName)
                                   ? model.SessionName
                                   : model.AcademicYear + " " + model.Semester + " Semester Timetable",
                    AcademicYear = model.AcademicYear,
                    Semester = model.Semester,
                    GeneratedBy = Session["FullName"]?.ToString() ?? "Admin",
                    TotalCourses = result.Count
                };

                int sessionId = _db.CreateSession(session);
                _db.SaveTimetableSession(result, sessionId);

                TempData["Success"] = model.Semester + " semester timetable generated — " +
                    result.Count + " courses scheduled.";
                return RedirectToAction("ViewSession", new { id = sessionId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Generation failed: " + ex.Message;
                return RedirectToAction("Generate");
            }
        }

        // POST /Timetable/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                _db.DeleteSession(id);
                TempData["Success"] = "Timetable session deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Cannot delete: " + ex.Message;
            }
            return RedirectToAction("History");
        }

        // GET /Timetable/History
        public ActionResult History()
        {
            ViewBag.Title = "Timetable History";
            return View(_db.GetAllSessions());
        }

        // GET /Timetable/Print/5
        public ActionResult Print(int? id)
        {
            TimetableSession session = null;

            if (id.HasValue)
            {
                session = _db.GetSessionById(id.Value);
            }
            else
            {
                // Fall back to most recent session
                var sessions = _db.GetAllSessions();
                if (sessions.Count > 0)
                {
                    session = sessions[0];
                    id = session.SessionId;
                }
            }

            if (session == null)
            {
                TempData["Error"] = "No timetable session found to print.";
                return RedirectToAction("Index");
            }

            ViewBag.Session = session;
            return View(_db.GetTimetableBySession(id.Value));
        }
        // GET /Timetable/EditEntry/5
        public ActionResult EditEntry(int id)
        {
            ViewBag.Title = "Edit Timetable Entry";
            ViewBag.Rooms = _db.GetAllRooms();
            ViewBag.Slots = _db.GetAllTimeSlots();
            var entry = _db.GetTimetableEntryById(id);
            if (entry == null) return HttpNotFound();
            return View(entry);
        }

        // POST /Timetable/EditEntry
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditEntry(int timetableId, int roomId, int timeId)
        {
            try
            {
                _db.UpdateTimetableEntry(timetableId, roomId, timeId);
                TempData["Success"] = "Timetable entry updated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}

