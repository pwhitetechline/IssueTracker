using IssueTracker.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.AddSingleton<FileStorageService>();

builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<NotificationService>();

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// CSP (strict, local assets only)
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] =
    "default-src 'self'; script-src 'self' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https://www.webdirectbrands.com; frame-ancestors 'none'; base-uri 'self'";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// Optional DB create/seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Ensure DB exists; replace with migrations in real prod
    db.Database.EnsureCreated();
    // DbSeeder.Seed(db); // if you have a seeder
}

app.Run();
