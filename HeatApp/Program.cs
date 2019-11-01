using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore;

namespace HeatApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            //.UseKestrel(o =>
            //{
            //    //o.ListenAnyIP(1883, l => l.UseMqtt());
            //    o.ListenAnyIP(5000); // default http pipeline
            //})
            //.UseSetting("https_port", "443")
            .UseIISIntegration()
            .UseStartup<Startup>();
    }
}
