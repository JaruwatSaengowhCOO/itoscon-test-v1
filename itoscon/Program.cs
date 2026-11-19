using itoscon.Data;
using itoscon.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
// ให้ Razor แสดงภาษาไทยตรง ๆ แทนการแปลงเป็น &#xE01; ...
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));

// Cookie authentication: ถ้ายังไม่ login จะถูกส่งไปหน้า Login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Authent/Login";
        options.LogoutPath = "/Authent/Logout";
        options.AccessDeniedPath = "/Authent/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;      // JavaScript เข้าถึง cookie ไม่ได้
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Production ต้องใช้ HTTPS เท่านั้น; Development ใช้ตามโปรโตคอลที่เปิด (http/https)
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });
builder.Services.AddAuthorization();

// Rate limit: จำกัดการพยายาม login ไม่เกิน 5 ครั้งต่อ IP ในช่วง 15 นาที
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, token) =>
    {
        var t = context.HttpContext.RequestServices.GetRequiredService<itoscon.Services.IStaticText>();
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(t["err.tooManyLogin"], token);
    };
});

builder.Services.AddSingleton<itoscon.Services.IStaticText, itoscon.Services.StaticText>();
var app = builder.Build();
Loc.Current = app.Services.GetRequiredService<IStaticText>();

// สร้าง/อัปเดตฐานข้อมูลด้วย EF Core Migrations และสร้างผู้ดูแลเริ่มต้น (ถ้าตั้งค่าไว้)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbInitializer.SeedInitialAdmin(db, builder.Configuration);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
var supportedCultures = new[] { new CultureInfo("th-TH"), new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("th-TH"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// หน้าแรกของระบบคือหน้า Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Authent}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
