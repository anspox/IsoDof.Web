using System.Diagnostics;

namespace IsoDof.Web.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Statik dosyaları (css, js, font, png vb.) log gürültüsü yapmasın diye atlayabiliriz
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        // Yanıt başlıkları gönderilmeden hemen önce header'ları ekle
        context.Response.OnStarting(() =>
        {
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            context.Response.Headers["X-Execution-Time-Ms"] = elapsedMs.ToString("0.0");
            context.Response.Headers["Server-Timing"] = $"total;dur={elapsedMs:0.0};desc=\"Server Processing\"";
            context.Items["ExecutionTimeMs"] = elapsedMs;
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var totalMs = stopwatch.Elapsed.TotalMilliseconds;
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            var user = context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "Anonim";

            if (totalMs >= 800)
            {
                _logger.LogWarning("🔴 [PERF-CRITICAL] YAVAŞ İŞLEM: {Method} {Path} => {StatusCode} süresi: {Elapsed:0.0} ms | Kullanıcı: {User}",
                    method, path, statusCode, totalMs, user);
            }
            else if (totalMs >= 300)
            {
                _logger.LogWarning("🟡 [PERF-WARN] DİKKAT: {Method} {Path} => {StatusCode} süresi: {Elapsed:0.0} ms | Kullanıcı: {User}",
                    method, path, statusCode, totalMs, user);
            }
            else
            {
                _logger.LogInformation("⚡ [PERF] {Method} {Path} => {StatusCode} ({Elapsed:0.0} ms)",
                    method, path, statusCode, totalMs);
            }
        }
    }
}
