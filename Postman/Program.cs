// Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Configures MVC, including views.
builder.Services.AddControllersWithViews();

// Register the DataParsingService as a transient service.
// Transient services are created each time they are requested.
builder.Services.AddTransient<DataParsingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Use exception handler for production environments.
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Redirects HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Enables static files (e.g., CSS, JavaScript, images) to be served.
app.UseStaticFiles();

// Configures routing for the application.
app.UseRouting();

// Enables authorization middleware.
app.UseAuthorization();

// Defines the default route pattern for MVC controllers.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Runs the application.
app.Run();
