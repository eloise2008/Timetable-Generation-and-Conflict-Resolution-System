using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;
using Timetabling.Models;

namespace Timetabling.Controllers
{
    public class CourseController : BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public ActionResult Index()
        {
            ViewBag.Title = "Courses";
            return View(_db.GetAllCourses());
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Add Course";
            ViewBag.Lecturers = _db.GetAllLecturers();
            ViewBag.Departments = _db.GetAllDepartments();
            return View(new Course());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(Course model)
        {
            try
            {
                _db.InsertCourse(model);
                TempData["Success"] = "Course '" + model.CourseCode + "' added successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error adding course: " + ex.Message;
                ViewBag.Lecturers = _db.GetAllLecturers();
                ViewBag.Departments = _db.GetAllDepartments();
                return View(model);
            }
        }

        public ActionResult Edit(int id)
        {
            ViewBag.Title = "Edit Course";
            ViewBag.Lecturers = _db.GetAllLecturers();
            ViewBag.Departments = _db.GetAllDepartments();
            var course = _db.GetCourseById(id);
            if (course == null) return HttpNotFound();
            return View(course);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(Course model)
        {
            try
            {
                _db.UpdateCourse(model);
                TempData["Success"] = "Course updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating course: " + ex.Message;
                ViewBag.Lecturers = _db.GetAllLecturers();
                ViewBag.Departments = _db.GetAllDepartments();
                return View(model);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                _db.DeleteCourse(id);
                TempData["Success"] = "Course deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Cannot delete: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
        public ActionResult AssignLecturer(int id)
        {
            ViewBag.Title = "Assign Lecturer";
            ViewBag.Lecturers = _db.GetAllLecturers();
            var course = _db.GetCourseById(id);
            if (course == null) return HttpNotFound();
            return View(course);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AssignLecturer(int courseId, int lecturerId)
        {
            try
            {
                _db.AssignCourseToLecturer(courseId, lecturerId);
                TempData["Success"] = "Lecturer assigned successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}