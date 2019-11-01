using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HeatApp.Models;
using HeatApp.Services;

namespace HeatApp.Controllers
{
    [Authorize]
    public class BoilerController : Controller
    {
        private readonly HeatAppContext db;
        private readonly CommandService commandService;

        public BoilerController(HeatAppContext db, CommandService commandService)
        {
            this.db = db;
            this.commandService = commandService;
        }

        public async Task<IActionResult> Index()
        {
            var config = await db.Configs.SingleAsync(c => c.ConfigID == "BoilerEnabled");
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("ConfigID, ConfigValue")] Config config)
        {
            if (ModelState.IsValid)
            {
                var be = await db.Configs.SingleOrDefaultAsync(b => b.ConfigID == "BoilerEnabled");
                if (be.ConfigValue != config.ConfigValue)
                {
                    be.ConfigValue = config.ConfigValue;
                    await db.SaveChangesAsync();
                }
            }
            return View(config);
        }

        [HttpGet]
        [Route("/state/kettle")]
        [AllowAnonymous]
        public async Task<string> BoilerAction(string state)
        {
            return commandService.GetKettleAction();
        }
    }
}
