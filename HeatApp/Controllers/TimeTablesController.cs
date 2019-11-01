using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HeatApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HeatApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace HeatApp.Controllers
{
    [Authorize]
    public class TimeTablesController : Controller
    {
        private readonly HeatAppContext db;
        private readonly CommandService commandService;

        public TimeTablesController(HeatAppContext db, CommandService commandService)
        {
            this.db = db;
            this.commandService = commandService;
        }

        // GET: TimeTables
        public async Task<IActionResult> Index()
        {
            return View(await db.Timetables.ToListAsync());
        }

        // GET: TimeTables/Create
        public IActionResult Create()
        {
            ViewBag.Types = new List<SelectListItem> { new SelectListItem { Value = "week", Text = "Týden" } };
            TimeTable timeTable = new TimeTable() { SleepTemp = 38, NoFrostTemp = 30, EcoTemp = 42, WarmTemp = 46 };
            return View("Edit", timeTable);
        }



        // GET: TimeTables/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeTable = await db.Timetables.FindAsync(id);
            if (timeTable == null)
            {
                return NotFound();
            }
            ViewBag.Types = new List<SelectListItem> { new SelectListItem { Value = "week", Text = "Týden" } };
            var res = db.Timers.Where(t => t.TimeTableId == id);
            var timers = new object[8, 8];
            foreach (var rec in res)
            {
                var d = rec.Idx >> 4;
                var t = rec.Idx & 0xf;


                var x = rec.Value;
                var rr = new { h = ((x & 0xfff) / 60), m = ((x & 0xfff) % 60), v = x >> 12 };
                if (rr.h < 24)
                {
                    timers[d, t] = rr;
                }

            }
            ViewBag.Timers = JsonConvert.SerializeObject(timers);
            return View("Edit", timeTable);
        }

        // POST: TimeTables/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Caption,Type,NoFrostTemp,SleepTemp,EcoTemp,WarmTemp,TimersSerialized")] TimeTable timeTable)
        {
            if (id != timeTable.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                commandService.SaveTimeTable(timeTable);
                return RedirectToAction(nameof(Index));
            }
            return View(timeTable);
        }

        // GET: TimeTables/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeTable = await db.Timetables
                .FirstOrDefaultAsync(m => m.Id == id);
            if (timeTable == null)
            {
                return NotFound();
            }

            return View(timeTable);
        }

        // POST: TimeTables/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var timers = db.Timers.Where(t => t.TimeTableId == id);
            db.RemoveRange(timers);

            var timeTable = await db.Timetables.FindAsync(id);
            db.Timetables.Remove(timeTable);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TimeTableExists(int id)
        {
            return db.Timetables.Any(e => e.Id == id);
        }
    }
}
