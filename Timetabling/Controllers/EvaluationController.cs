using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;
using Timetabling.Services;

namespace Timetabling.Controllers
{
    public class EvaluationController: BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();
        private readonly MetricsCalculator _calc = new MetricsCalculator();

        public ActionResult Index(int? sessionId)
        {
            ViewBag.Title = "Evaluation Metrics";
            ViewBag.Sessions = _db.GetAllSessions();

            var sessions = _db.GetAllSessions();
            if (sessions.Count == 0)
            {
                TempData["Error"] = "No timetable sessions found. Generate one first.";
                return View((Timetabling.Models.EvaluationMetrics)null);
            }

            var targetId = sessionId ?? sessions[0].SessionId;
            var session = _db.GetSessionById(targetId);
            var assignments = _db.GetTimetableBySession(targetId);
            var allCourses = session != null
                ? _db.GetCoursesBySemester(session.Semester)
                : _db.GetAllCourses();

            var metrics = _calc.Calculate(assignments, allCourses, session);
            ViewBag.CurrentSessionId = targetId;

            return View(metrics);
        }
    }
}