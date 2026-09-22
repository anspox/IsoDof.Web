using IsoDof.Web.Controllers;

namespace IsoDof.Web.Middleware;

/// <summary>
/// Geçici şifreyle giriş yapan kullanıcıyı, şifresini değiştirene kadar şifre değiştirme sayfasına yönlendirir.
/// </summary>
public class ForcePasswordChangeMiddleware
{
    private static readonly string[] AllowedPaths =
    {
        "/Account/ChangePassword",
        "/Account/Logout",
        "/Account/AccessDenied",
        "/Home/Privacy",
        "/Home/Error",
    };

    private readonly RequestDelegate _next;

    public ForcePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim(AccountController.MustChangePasswordClaim, "true"))
        {
            var path = context.Request.Path;
            var allowed = AllowedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
            if (!allowed)
            {
                context.Response.Redirect(context.Request.PathBase + "/Account/ChangePassword");
                return Task.CompletedTask;
            }
        }

        return _next(context);
    }
}
