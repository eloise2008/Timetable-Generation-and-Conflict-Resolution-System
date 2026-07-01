using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Timetabling.Data;
using Timetabling.Models;

namespace Timetabling.Controllers
{
    public class RoomController : BaseController
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public ActionResult Index()
        {
            ViewBag.Title = "Rooms";
            return View(_db.GetAllRooms());
        }

        public ActionResult Create()
        {
            ViewBag.Title = "Add Room";
            return View(new Room());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(Room model)
        {
            try
            {
                _db.InsertRoom(model);
                TempData["Success"] = "Room '" + model.RoomName + "' added.";
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
            ViewBag.Title = "Edit Room";
            var room = _db.GetRoomById(id);
            if (room == null) return HttpNotFound();
            return View(room);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(Room model)
        {
            try
            {
                _db.UpdateRoom(model);
                TempData["Success"] = "Room updated.";
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
                _db.DeleteRoom(id);
                TempData["Success"] = "Room deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Cannot delete: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}