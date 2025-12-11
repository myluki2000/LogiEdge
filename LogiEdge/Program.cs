using Havit.Blazor.Components.Web;
using LogiEdge.Areas.Identity;
using LogiEdge.BaseService;
using LogiEdge.WebUI.Customers;
using LogiEdge.CustomerService;
using LogiEdge.Data;
using LogiEdge.ExcelImporterService;
using LogiEdge.Shared;
using LogiEdge.WebUI.Warehouse;
using LogiEdge.WarehouseService;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var appIdentityConnectionString = builder.Configuration.GetConnectionString("AppIdentityConnection") 
                                              ?? throw new InvalidOperationException("Connection string 'AppIdentityConnection' not found.");
            builder.Services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseNpgsql(appIdentityConnectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<AppIdentityDbContext>();
            builder.Services.AddHxServices();
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            builder.Services.AddSingleton<ServiceModuleConfigurationCollection>();

            // modules
            List<IServiceModuleConfiguration> modules =
            [
                new BaseServiceModuleConfiguration(),
                new CustomerManagementModuleConfiguration(),
                new CustomerServiceModuleConfiguration(),
                new WarehouseModuleConfiguration(),
                new WarehouseServiceModuleConfiguration(),
                new ExcelImporterServiceModuleConfiguration()
            ];

            // add services of modules
            foreach (IServiceModuleConfiguration module in modules)
            {
                module.RegisterServices(builder);
            }

            // auto mapper profiles
            builder.Services.AddAutoMapper(cfg => { }, [typeof(Program).Assembly, ..modules.Select(x => x.Assembly).Distinct()]);

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