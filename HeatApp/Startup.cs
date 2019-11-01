using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using MQTTnet.Server;
using MQTTnet;
using System.Text;
using HeatApp.Services;
using Microsoft.Extensions.Hosting;
using HeatApp.Models;
using MQTTnet.Client;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.HttpOverrides;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace HeatApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

            });
            services.AddControllersWithViews();

            //services.AddDbContext<HeatAppContext>(options =>
            //    options.UseSqlite(
            //        Configuration.GetConnectionString("DefaultConnection")));

            services.AddDbContext<HeatAppContext>(options =>
                options.UseMySql(
                    Configuration.GetConnectionString("MySql"),
                    mySqlOptions =>
                    {
                        mySqlOptions.ServerVersion(new Version(10, 3, 17), ServerType.MariaDb); // replace with your Server Version and Type
                    }));


            services.AddDefaultIdentity<IdentityUser>()
                .AddEntityFrameworkStores<HeatAppContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<CommandService>();
            Configuration.GetSection("MqttClient").GetValue<bool>("UseMqttClient");
            {
                services.AddHostedService<MqttService>();
            }
            Configuration.GetSection("Serial").GetValue<bool>("UseSerial");
            {
                services.AddHostedService<SerialReadService>();
            }

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider services)
        {
            app.UseRouting();
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(new CultureInfo("cs-CZ")),
                SupportedCultures = new List<CultureInfo> { new CultureInfo("cs-CZ") }
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();


            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });

            CreateDefaultUser(services).Wait();
        }

        private async Task<object> CreateDefaultUser(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            IdentityUser user = await userManager.FindByNameAsync("admin");
            if (user == null)
            {
                user = new IdentityUser { UserName = "admin" };
                var result = await userManager.CreateAsync(user, "minda");
            }
            return Task.CompletedTask;
        }
    }
}
