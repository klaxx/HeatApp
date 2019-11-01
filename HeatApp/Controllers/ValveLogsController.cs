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
    public class ValveLogsController : Controller
    {
        private readonly HeatAppContext db;

        public ValveLogsController(HeatAppContext db)
        {
            this.db = db;
        }

        // GET: ValveLogs
        public async Task<IActionResult> Index()
        {
            var items = await db.ValveLog.ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<object> LoadData()
        {
            try
            {
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = await db.ValveLog.CountAsync();

                var data = await db.ValveLog.OrderByDescending(a => a.Time).Skip(skip).Take(pageSize).Select(d => new {
                    time = d.Time.ToString(),
                    addr = d.Addr,
                    actual = d.Actual.ToString("F2"),
                    battery =  d.Battery.ToString("F3"), 
                    error = d.Error,
                    locked = d.Locked,
                    turn = d.Turn.ToString() + "%",
                    wanted = d.Wanted.ToString("F1"),
                    window = d.Window,
                    mode = d.Auto
                }).ToListAsync();
                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
