using itoscon.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// ให้ Razor แสดงภาษาไทยตรง ๆ แทนการแปลงเป็น &#xE01; ...
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

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
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "พยายามเข้าสู่ระบบบ่อยเกินไป กรุณารอ 15 นาทีแล้วลองใหม่", token);
    };
});

var app = builder.Build();

// สร้างฐานข้อมูล, เพิ่มคอลัมน์ Password ถ้ายังไม่มี, และสร้างผู้ดูแลเริ่มต้น (ถ้าตั้งค่าไว้)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbInitializer.EnsureSchema(db);
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
