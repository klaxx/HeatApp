using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(HeatApp.Areas.Identity.IdentityHostingStartup))]
namespace HeatApp.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}