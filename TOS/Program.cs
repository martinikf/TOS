using System.Configuration;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TOS.Data;
using TOS.Models;
using TOS.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => options
        .UseLazyLoadingProxies()
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

//DateTime - postgres compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();

builder.Services.AddLocalization();

builder.Services.Configure<RequestLocalizationOptions>(
    options =>
    {
        var supportedCultures = new[]
        {
            new CultureInfo("cz"),
            new CultureInfo("en")
        };
        
        options.DefaultRequestCulture = new RequestCulture("cz");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
    });

builder.Services.AddScoped<SmtpEmailSenderSettings, SmtpEmailSenderSettings>(
    _ => new SmtpEmailSenderSettings()
    {
        Username = builder.Configuration.GetSection("EmailSettings").GetValue<string>("Username") ?? throw new ConfigurationErrorsException("SMTP-Username is not set in appsettings.json"),
        Password = builder.Configuration.GetSection("EmailSettings").GetValue<string>("Password") ?? throw new ConfigurationErrorsException("SMTP-Password is not set in appsettings.json"),
        Port = builder.Configuration.GetSection("EmailSettings").GetValue<int>("Port"),
        FromAddress = builder.Configuration.GetSection("EmailSettings").GetValue<string>("FromAddress") ?? throw new ConfigurationErrorsException("SMTP-FromAddress is not set in appsettings.json"),
        SmtpServer = builder.Configuration.GetSection("EmailSettings").GetValue<string>("SmtpServer") ?? throw new ConfigurationErrorsException("SMTP-SmtpServer is not set in appsettings.json"),
        EnableSsl = builder.Configuration.GetSection("EmailSettings").GetValue<bool>("EnableSsl")
    });
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddScoped<INotificationManager, NotificationManager>();

var app = builder.Build();

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

if(true)
    Seed.InfUpolSeed(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
