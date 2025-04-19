using MarketPlaceInfrastructure;
using MarketPlaceDomain.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using MarketPlaceInfrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OlxContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<OlxContext>()
    .AddDefaultTokenProviders();

builder.Services.AddTransient<IEmailSender, FakeEmailSender>();


builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorPages();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<IImportService<Product>, ProductsImportService>();
builder.Services.AddScoped<IExportService<Product>, ProductsExportService>();
builder.Services.AddScoped<IDataPortServiceFactory, DataPortServiceFactory>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
    }
    if (!await roleManager.RoleExistsAsync("User"))
    {
        await roleManager.CreateAsync(new ApplicationRole { Name = "User" });
    }

    var adminUser = await userManager.FindByEmailAsync("Admin1@example.com");
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "Admin1@example.com",
            Email = "Admin1@example.com"
        };
        await userManager.CreateAsync(adminUser, "Admin1@example.com");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
    
app.Run();
