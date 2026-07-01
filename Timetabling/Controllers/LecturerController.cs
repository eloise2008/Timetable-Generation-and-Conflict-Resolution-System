using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;
using Timetabling.Models;

namespace Timetabling.Controllers
{
    public class LecturerController : BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public ActionResult Index()
        {
            ViewBag.Title = "Lecturers";
            return View(_db.GetAllLecturers());
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Add Lecturer";
            ViewBag.Departments = _db.GetAllDepartments();
            return View(new Lecturer());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(Lecturer model)
        {
            try
            {
                _db.InsertLecturer(model);
                TempData["Success"] = "'" + model.LecturerName + "' added successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                ViewBag.Departments = _db.GetAllDepartments();
                return View(model);
            }
        }

        public ActionResult Edit(int id)
        {
            ViewBag.Title = "Edit Lecturer";
            ViewBag.Departments = _db.GetAllDepartments();
            var lec = _db.GetLecturerById(id);
            if (lec == null) return HttpNotFound();
            return View(lec);
        }
        public ActionResult Courses(int id)
        {
            ViewBag.Title = "Assign Courses";
            var lecturer = _db.GetLecturerById(id);
            if (lecturer == null) return HttpNotFound();
            ViewBag.Lecturer = lecturer;
            ViewBag.AllCourses = _db.GetAllCourses();
            ViewBag.MyCourses = _db.GetCoursesByLecturer(id);
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AssignCourse(int lecturerId, int courseId)
        {
            try
            {
                _db.AssignCourseToLecturer(courseId, lecturerId);
                TempData["Success"] = "Course assigned successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Courses", new { id = lecturerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UnassignCourse(int lecturerId, int courseId)
        {
            try
            {
                _db.UnassignCourseFromLecturer(courseId);
                TempData["Success"] = "Course unassigned.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Courses", new { id = lecturerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(Lecturer model)
        {
            try
            {
                _db.UpdateLecturer(model);
                TempData["Success"] = "Lecturer updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                ViewBag.Departments = _db.GetAllDepartments();
                return View(model);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                _db.DeleteLecturer(id);
                TempData["Success"] = "Lecturer deleted.";
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("FK_Course_Lecturer") ||
                    ex.Message.Contains("REFERENCE"))
                {
                    TempData["Error"] = "Cannot delete this lecturer because they " +
                        "have courses assigned. Please reassign those courses to " +
                        "another lecturer first, then delete.";
                }
                else
                {
                    TempData["Error"] = "Cannot delete: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ReassignCourse(int courseId, int fromLecturerId, int toLecturerId)
        {
            try
            {
                _db.ReassignCourse(courseId, toLecturerId);
                TempData["Success"] = "Course reassigned successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Courses", new { id = fromLecturerId });
        }

    }
}