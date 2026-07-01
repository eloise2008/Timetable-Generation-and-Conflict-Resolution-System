using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;
using Timetabling.Models;

namespace Timetabling.Controllers
{
    public class DepartmentController : BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public ActionResult Index()
        {
            ViewBag.Title = "Departments";
            return View(_db.GetAllDepartments());
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Add Department";
            return View(new Department());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(Department model)
        {
            try
            {
                _db.InsertDepartment(model);
                TempData["Success"] = "'" + model.DepartmentName + "' added.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(model);
            }
        }

        public ActionResult Edit(int id)
        {
            ViewBag.Title = "Edit Department";
            var dept = _db.GetDepartmentById(id);
            if (dept == null) return HttpNotFound();
            return View(dept);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(Department model)
        {
            try
            {
                _db.UpdateDepartment(model);
                TempData["Success"] = "Department updated.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View(model);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                _db.DeleteDepartment(id);
                TempData["Success"] = "Department deleted.";
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("FK_Lecturer_Department") ||
                    ex.Message.Contains("REFERENCE"))
                {
                    TempData["Error"] = "Cannot delete this department because it " +
                        "has lecturers or courses assigned. " +
                        "Please reassign them first.";
                }
                else
                {
                    TempData["Error"] = "Cannot delete: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }
    }
}