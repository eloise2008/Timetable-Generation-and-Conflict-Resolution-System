using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;
using Timetabling.Models;

namespace Timetabling.Controllers
{
    public class TimeSlotController : BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public ActionResult Index()
        {
            ViewBag.Title = "Time Slots";
            return View(_db.GetAllTimeSlots());
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Add Time Slot";
            return View(new TimeSlot());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(TimeSlot model)
        {
            try
            {
                _db.InsertTimeSlot(model);
                TempData["Success"] = "Time slot added.";
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
            ViewBag.Title = "Edit Time Slot";
            var slot = _db.GetTimeSlotById(id);
            if (slot == null) return HttpNotFound();
            return View(slot);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(TimeSlot model)
        {
            try
            {
                _db.UpdateTimeSlot(model);
                TempData["Success"] = "Time slot updated.";
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
                _db.DeleteTimeSlot(id);
                TempData["Success"] = "Time slot deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Cannot delete: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}