using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HeatApp.Models;
using HeatApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace HeatApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly CommandService commandService;
        private readonly HeatAppContext db;

        public HomeController(HeatAppContext db, CommandService commandService)
        {
            this.commandService = commandService;
            this.db = db;
        }

        public async Task<IActionResult> Index()
        {

            List<ValveView> log = commandService.GetValveStates();
            ViewBag.Valves = log.Count();
            ViewBag.ValvesOnline = log.Where(l => l.OnLine).Count();
            ViewBag.Requests = log.Where(l => l.OnLine && l.Turn > 40 && l.BoilerEnabled && l.Actual < l.Wanted && ((l.Wanted - l.Actual) > (decimal)0.25)).Count();
            ViewBag.Requestable = log.Where(l => l.OnLine && l.BoilerEnabled).Count();
            ViewBag.Boiler = commandService.GetKettleAction();
            ViewBag.Queue = commandService.GetQueueCount();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
