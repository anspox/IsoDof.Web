using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using IsoDof.Web.Data;
using IsoDof.Web.Models;
using IsoDof.Web.Models.Entities;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace IsoDof.Web.Controllers;

public class AccountController : Controller
{
    /// <summary>Bu kadar art arda hatalı denemeden sonra hesap kilitlenir.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Kilit süresi.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Şifre değişikliği zorunlu olan oturumlara eklenen claim.</summary>
    public const string MustChangePasswordClaim = "MustChangePassword";

    private readonly AppDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.AppUsers
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            var minutes = (int)Math.Ceiling((user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes);
            ModelState.AddModelError(string.Empty,
                $"Çok fazla hatalı deneme yapıldığı için hesabınız geçici olarak kilitlendi. Lütfen {minutes} dakika sonra tekrar deneyin.");
            return View(model);
        }

        var hasher = new PasswordHasher<AppUser>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
                _logger.LogWarning("Hesap kilitlendi: kullanıcı {UserId}, IP {Ip}",
                    user.Id, HttpContext.Connection.RemoteIpAddress);
            }
            await _context.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, model.Password);
        }
        await _context.SaveChangesAsync();

        await SignInAsync(user);

        if (user.MustChangePassword)
        {
            return RedirectToAction(nameof(ChangePassword), new { returnUrl });
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.IsForced = User.HasClaim(MustChangePasswordClaim, "true");
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.IsForced = User.HasClaim(MustChangePasswordClaim, "true");

        if (!PasswordPolicy.IsValid(model.NewPassword, out var policyError))
        {
            ModelState.AddModelError(nameof(model.NewPassword), policyError!);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _context.AppUsers.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction(nameof(Login));
        }

        var hasher = new PasswordHasher<AppUser>();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mevcut şifre hatalı.");
            return View(model);
        }

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, model.NewPassword) != PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Yeni şifre mevcut şifreyle aynı olamaz.");
            return View(model);
        }

        user.PasswordHash = hasher.HashPassword(user, model.NewPassword);
        user.MustChangePassword = false;
        await _context.SaveChangesAsync();

        // Oturumu, "şifre değiştirmeli" işareti olmadan yeniden oluştur.
        await SignInAsync(user);

        TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToAction("Login", "Account");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        if (user.DepartmentId.HasValue)
        {
            claims.Add(new Claim("DepartmentId", user.DepartmentId.Value.ToString()));
            if (user.Department != null)
            {
                claims.Add(new Claim("DepartmentName", user.Department.Name));
            }
        }

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(MustChangePasswordClaim, "true"));
        }

        var identity = new ClaimsIdentity(claims, "Cookies");
        await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(identity));
    }
}
