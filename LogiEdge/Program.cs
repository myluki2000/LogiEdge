using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using LogiEdge.ApiAuth;
using LogiEdge.Areas.Identity;
using LogiEdge.BaseService;
using LogiEdge.WebUI.Customers;
using LogiEdge.CustomerService;
using LogiEdge.Data;
using LogiEdge.Identity;
using LogiEdge.ExcelImporterService;
using LogiEdge.Service.DocumentGenerator;
using LogiEdge.Service.PlanningBoard;
using LogiEdge.Shared;
using LogiEdge.Shared.ApiAuth;
using LogiEdge.WebUI.DocumentGenerator;
using LogiEdge.WebUI.PlanningBoard;
using LogiEdge.WebUI.Warehouse;
using LogiEdge.WarehouseService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogiEdge
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var appIdentityConnectionString = builder.Configuration.GetConnectionString("AppIdentityConnection") 
                                              ?? throw new InvalidOperationException("Connection string 'AppIdentityConnection' not found.");
            // Factory registration (also registers the context itself as scoped) so host services can
            // resolve the identity database outside of a request/circuit scope.
            builder.Services.AddDbContextFactory<AppIdentityDbContext>(options =>
                options.UseNpgsql(appIdentityConnectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                if (builder.Environment.IsDevelopment())
                {
                    // Allow simple test credentials (e.g. admin/admin) in Development only.
                    options.Password.RequiredLength = 4;
                    options.Password.RequireDigit = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireLowercase = false;
                }
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppIdentityDbContext>();
            // Bearer token scheme for programmatic API access ("lget_\u2026" tokens); endpoints opt in explicitly.
            builder.Services.AddAuthentication()
                .AddScheme<ApiTokenSchemeOptions, ApiTokenAuthenticationHandler>(AppAuthSchemes.Token, _ => { });
            builder.Services.AddHxServices();
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddHxMessageBoxHost();
            builder.Services.AddAuthorization(options =>
            {
                // Default-deny: every endpoint requires sign-in unless explicitly marked [AllowAnonymous].
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
            // The composite provider reports HttpContext.User for HTTP requests (cookie or API token) and
            // delegates to the revalidating Identity provider for Blazor circuits.
            builder.Services.AddScoped<RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            builder.Services.AddScoped<AuthenticationStateProvider, BlazorOrHttpAuthenticationStateProvider>();
            builder.Services.AddSingleton<ISystemUserProvider, SystemUserProvider>();
            builder.Services.AddSingleton<IUserDirectory, UserDirectory>();
            builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();
            builder.Services.Configure<ApiTokensOptions>(builder.Configuration.GetSection(ApiTokensOptions.SectionName));

            builder.Services.AddSingleton<ServiceModuleConfigurationCollection>();

            // modules
            List<IServiceModuleConfiguration> modules =
            [
                new BaseServiceModuleConfiguration(),
                new CustomerManagementModuleConfiguration(),
                new CustomerServiceModuleConfiguration(),
                new WarehouseModuleConfiguration(),
                new WarehouseServiceModuleConfiguration(),
                new ExcelImporterServiceModuleConfiguration(),
                new PlanningBoardServiceModuleConfiguration(),
                new PlanningBoardWebUiModuleConfiguration(),
                new DocumentGeneratorServiceModuleConfiguration(),
                new DocumentGeneratorWebUiModuleConfiguration(),
            ];

            // add services of modules
            foreach (IServiceModuleConfiguration module in modules)
            {
                module.RegisterServices(builder);
            }

            // auto mapper profiles
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(modules.Select(x => x.Assembly).Distinct());
            }, [typeof(Program).Assembly, ..modules.Select(x => x.Assembly).Distinct()]);
            
            // build app
            var app = builder.Build();

            // add modules to our module list stored as a singleton service
            app.Services.GetService<ServiceModuleConfigurationCollection>()!.AddRange(modules);

            foreach (IServiceModuleConfiguration module in modules)
            {
                module.OnAppBuilt(app);
            }

            // Seed the Admin role + local test users in Development (admin@localhost / admin, user@localhost / 123456) - see .notes/auth-impl.md
            if (app.Environment.IsDevelopment())
            {
                await TestUserSeeder.SeedAsync(app);
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            await app.RunAsync();
        }
    }
}
