using System.Diagnostics;
using System.Reflection;
using LogiEdge.Areas.Identity;
using LogiEdge.CustomerManagement;
using LogiEdge.CustomerService;
using LogiEdge.Data;
using LogiEdge.Shared;
using LogiEdge.Warehouse;
using LogiEdge.WarehouseService;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            builder.Services.AddSingleton<WeatherForecastService>();

            builder.Services.AddSingleton<ServiceModuleConfigurationCollection>();

            // modules
            List<IServiceModuleConfiguration> modules = new()
            {
                new WarehouseModuleConfiguration(),
                new WarehouseServiceModuleConfiguration(),
                new CustomerManagementModuleConfiguration(),
                new CustomerServiceModuleConfiguration()
            };

            // add services of modules
            foreach (IServiceModuleConfiguration module in modules)
            {
                module.RegisterServices(builder);
            }

            // build app
            var app = builder.Build();

            // add modules to our module list stored as a singleton service
            app.Services.GetService<ServiceModuleConfigurationCollection>()!.AddRange(modules);

            foreach (IServiceModuleConfiguration module in modules)
            {
                module.OnAppBuilt(app);
            }

            // add module assemblies

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllers();
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}