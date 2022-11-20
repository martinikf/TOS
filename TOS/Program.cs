using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddControllersWithViews();

//Register email sender
builder.Services.AddSingleton<SmtpEmailSenderSettings, SmtpEmailSenderSettings>(
    _ => new SmtpEmailSenderSettings()
    {
        Username = builder.Configuration["SendinBlue:Username"] ?? throw new Exception("Secrets are not set"),
        Password = builder.Configuration["SendinBlue:Password"] ?? throw new Exception("Secrets are not set"),
        Port = 587,
        FromAddress = "noreply@tos.tos",
        SmtpServer = "smtp-relay.sendinblue.com",
        EnableSsl = true
    });
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

var app = builder.Build();

if(true)
    Seed.InitSeed(app);

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
