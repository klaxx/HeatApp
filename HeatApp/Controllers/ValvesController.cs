using HeatApp.Models;
using HeatApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeatApp.Controllers
{
    [Authorize]
    public class ValvesController : Controller
    {
        private readonly HeatAppContext db;
        private readonly CommandService commandService;

        public ValvesController(HeatAppContext db, CommandService commandService)
        {
            this.commandService = commandService;
            this.db = db;
        }

        // GET: Valves
        public async Task<IActionResult> Index()
        {
            List<ValveView> vlog = await (from v in db.Valves
                                          join ll in db.ValveLog.GroupBy(vl => vl.Addr).Select(g => new { Addr = (int?)g.Key, Id = (int?)g.Max(p => p.Id) }) on v.Addr equals ll.Addr into lljoin
                                          from ll in lljoin.DefaultIfEmpty()
                                          join vl in db.ValveLog on ll.Id equals vl.Id into vljoin
                                          from vl in vljoin.DefaultIfEmpty()
                                          join tt in db.Timetables on v.TimeTable equals tt.Id into ttjoin
                                          from tt in ttjoin.DefaultIfEmpty()
                                          select new ValveView(v, vl, tt)).ToListAsync();
            //var vlog = await commandService.GetValveStates();
            return View(vlog);
        }

        [HttpGet]
        public void LoadSettings(int? id)
        {
            if (id != null)
            {
                commandService.GetSettings(id.Value);
            }
        }

        public object GetSettings(int? id)
        {
            var memoryLayout = commandService.GetMemoryLayout;
            var settings = (from ml in memoryLayout
                            join se in db.ValveSettings.Where(v => v.Addr == id) on ml.Value.idx equals se.Idx into mjoin
                            from se in mjoin.DefaultIfEmpty()
                            select new { Name = ml.Key, Description = ml.Value.description, Category = ml.Value.category, Value = (se != null ? se.Value : -1) });
            return settings;
        }

        [HttpGet]
        public async Task<object> GetValveLog(int? id)
        {
            if (id != null)
            {
                var log = await db.ValveLog.Where(l => l.Addr == id.Value && l.Time.AddDays(2) > DateTime.Now).Select(s => new { s.Time, s.Wanted, s.Turn, s.Actual }).ToListAsync();
                return new { Time = log.Select(t => new DateTimeOffset(t.Time).ToUnixTimeMilliseconds()), Measured = log.Select(m => m.Actual), Wanted = log.Select(w => w.Wanted), Turn = log.Select(t => t.Turn) };
            }
            return null;
        }

        // GET: Valves/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            List<SelectListItem> timetables = new List<SelectListItem> { new SelectListItem() { Value = "0", Text = "žádný" } };
            timetables.AddRange(await db.Timetables.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Caption }).ToListAsync());
            ViewBag.TimeTables = timetables;

            if (id == null)
            {
                return View(new ValveView());
            }
            var lastLog = await db.ValveLog.Where(v => v.Addr == id.Value).OrderByDescending(t => t.Time).FirstOrDefaultAsync();
            var valve = await db.Valves.SingleOrDefaultAsync(v => v.Addr == id.Value);
            if (valve == null)
            {
                return NotFound();
            }
            var valveView = new ValveView(valve, lastLog, null);
            return View(valveView);
        }

        // POST: Valves/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Addr,Caption,BoilerEnabled,Temperature,TimeTable,Wanted,Auto,Locked")] ValveView valveView)
        {
            if (ModelState.IsValid && valveView.Addr > 0)
            {
                var valve = await db.Valves.SingleOrDefaultAsync(v => v.Addr == valveView.Addr);
                if (valve == null)
                {
                    valve = new Valve() { Addr = valveView.Addr, BoilerEnabled = valveView.BoilerEnabled, Caption = valveView.Caption, TimeTable = valveView.TimeTable, Locked = valveView.Locked };
                    await db.Valves.AddAsync(valve);
                    await db.SaveChangesAsync();
                    commandService.SendTimetableToValve(valve.Addr, valve.TimeTable);
                }
                else
                {
                    bool change = false;
                    if (valve.Caption != valveView.Caption)
                    {
                        valve.Caption = valveView.Caption;
                        change = true;
                    }
                    if (valve.Addr != valveView.Addr)
                    {
                        valve.Addr = valveView.Addr;
                        change = true;
                    }
                    if (valve.BoilerEnabled != valveView.BoilerEnabled)
                    {
                        valve.BoilerEnabled = valveView.BoilerEnabled;
                        change = true;
                    }
                    if (valve.Locked != valveView.Locked)
                    {
                        valve.Locked = valveView.Locked;
                        change = true;
                    }
                    if (change)
                    {
                        await db.SaveChangesAsync();
                    }
                    if (valve.TimeTable != valveView.TimeTable)
                    {
                        valve.TimeTable = valveView.TimeTable;
                        await db.SaveChangesAsync();
                        commandService.SendTimetableToValve(valve.Addr, valve.TimeTable);
                    }
                }
                commandService.UpdateValve(valveView);
                return RedirectToAction(nameof(Index));
            }
            List<SelectListItem> timetables = new List<SelectListItem> { new SelectListItem() { Value = "0", Text = "žádný" } };
            timetables.AddRange(await db.Timetables.Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Caption }).ToListAsync());
            ViewBag.TimeTables = timetables;
            return View(valveView);
        }

        // GET: Valves/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var valve = await db.Valves.FirstOrDefaultAsync(m => m.Addr == id);
            if (valve == null)
            {
                return NotFound();
            }
            return View(valve);
        }

        // POST: Valves/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var valve = await db.Valves.FindAsync(id);
            db.Valves.Remove(valve);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ValveExists(int id)
        {
            return db.Valves.Any(e => e.Addr == id);
        }
    }
}
