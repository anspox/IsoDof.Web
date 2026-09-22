namespace IsoDof.Web.Middleware;

/// <summary>
/// Her yanıta tarayıcı güvenlik başlıklarını ekler (clickjacking, MIME sniffing, XSS ve veri sızıntısı koruması).
/// </summary>
public class SecurityHeadersMiddleware
{
    // Görünümlerde satır içi <script> ve onclick kullanıldığı için 'unsafe-inline' gereklidir.
    // Buna rağmen politika, dış kaynaklı betikleri, çerçeveye gömülmeyi ve başka sitelere form gönderimini engeller.
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "font-src 'self'; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            // Dahili uygulama: arama motorları indekslemesin.
            headers["X-Robots-Tag"] = "noindex, nofollow";
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers["Content-Security-Policy"] = ContentSecurityPolicy;
            }
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            return Task.CompletedTask;
        });

        return _next(context);
    }
}
