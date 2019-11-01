using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HeatApp.Models;

namespace HeatApp.Controllers
{
    [Authorize]
    public class QueueController : Controller
    {
        private readonly HeatAppContext db;

        public QueueController(HeatAppContext context)
        {
            db = context;
        }

        // GET: Queue
        public async Task<IActionResult> Index()
        {
            return View(await db.Queue.ToListAsync());
        }

        public async Task<IActionResult> ClearQueue()
        {
            db.Queue.RemoveRange(db.Queue);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // GET: Valves/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Valves/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Addr,Data")] Command command)
        {
            if (ModelState.IsValid)
            {
                command.Time = DateTime.Now;
                db.Queue.Add(command);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(command);
        }
        // GET: Queue/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var command = await db.Queue
                .FirstOrDefaultAsync(m => m.Id == id);
            if (command == null)
            {
                return NotFound();
            }

            return View(command);
        }

        // POST: Queue/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var command = await db.Queue.FindAsync(id);
            db.Queue.Remove(command);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CommandExists(int id)
        {
            return db.Queue.Any(e => e.Id == id);
        }
    }
}
