using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Middleware;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

using IsoDof.Web.Services.Logging;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFileLogging("logs");

// Sunucu yazılımını ifşa eden "Server: Kestrel" başlığını gönderme.
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

// Üretimde çerezler yalnızca HTTPS üzerinden gönderilir; geliştirmede HTTP profili de çalışsın.
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

// Hata izleme: yalnızca "Sentry:Dsn" ayarı (veya SENTRY_DSN ortam değişkeni) tanımlıysa etkinleşir.
var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? Environment.GetEnvironmentVariable("SENTRY_DSN");
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.SendDefaultPii = false; // KVKK: kişisel veriyi (IP, kullanıcı adı, çerez) gönderme
        options.TracesSampleRate = 0;
    });
}

// Türkçe karakterler (ı, ş, ğ...) HTML çıktısında &#x131; gibi varlıklara dönüştürülmeden yazılsın.
// HTML için tehlikeli karakterler (<, >, &, ") yine kodlanır.
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options =>
    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));

        var p = options.ModelBindingMessageProvider;
        p.SetValueIsInvalidAccessor(v => $"'{v}' değeri geçersiz.");
        p.SetValueMustNotBeNullAccessor(v => "Bu alan zorunludur.");
        p.SetAttemptedValueIsInvalidAccessor((v, f) => $"'{v}' değeri '{f}' alanı için geçersiz.");
        p.SetMissingBindRequiredValueAccessor(f => $"'{f}' alanı zorunludur.");
        p.SetMissingKeyOrValueAccessor(() => "Bu alan zorunludur.");
        p.SetNonPropertyValueMustBeANumberAccessor(() => "Bir sayı giriniz.");
        p.SetValueMustBeANumberAccessor(f => $"'{f}' alanına bir sayı giriniz.");
        p.SetUnknownValueIsInvalidAccessor(f => $"'{f}' alanına girilen değer geçersiz.");
    });

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "IsoDof.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // 8 saat işlem yapılmazsa oturum kapanır; kullanımda oldukça süre uzar.
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.HttpOnly = true;
});

// Kaba kuvvet saldırılarına karşı: aynı IP adresinden dakikada en fazla 10 giriş denemesi.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Çok fazla giriş denemesi yapıldı. Lütfen bir dakika sonra tekrar deneyin.", token);
    };
});

// Uygulama IIS / Nginx gibi bir ters vekil sunucunun arkasında çalışıyorsa gerçek istemci IP'si ve HTTPS bilgisi için.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// HSTS: tarayıcı bu siteye 1 yıl boyunca yalnızca HTTPS ile bağlanır.
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IDofReportService, DofReportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserImportParser, UserImportParser>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddHostedService<DofReminderBackgroundService>();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 404 gibi gövdesiz hata yanıtlarında kullanıcıya boş sayfa yerine hata sayfası göster.
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();

// Eski sürümde ekler wwwroot/uploads altına kaydediliyordu. Bu dosyalar artık yalnızca
// yetki kontrollü Dofs/Attachment aksiyonu üzerinden sunulur; doğrudan erişim engellenir.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // asp-append-version ile sürümlenen dosyalar 1 yıl, diğerleri 1 gün tarayıcı önbelleğinde tutulur.
        var versioned = ctx.Context.Request.Query.ContainsKey("v");
        ctx.Context.Response.Headers.CacheControl = versioned
            ? "public,max-age=31536000,immutable"
            : "public,max-age=86400";
    }
});

app.UseMiddleware<RequestTimingMiddleware>();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<ForcePasswordChangeMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Entegrasyon testlerinin (WebApplicationFactory) Program sınıfına erişebilmesi için.
public partial class Program { }
