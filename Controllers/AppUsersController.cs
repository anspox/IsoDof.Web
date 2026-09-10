using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
using IsoDof.Web.Models;
using IsoDof.Web.Models.Entities;
using IsoDof.Web.Models.Entities.Enums;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace IsoDof.Web.Controllers;
[Authorize(Roles = "Admin")]
public class AppUsersController : Controller
{
    // Toplu içe aktarımda şifre verilmeyen kullanıcılar için varsayılan şifre
    public const string DefaultBulkPassword = "Sirket2026!";

    private readonly AppDbContext _context;
    private readonly IUserImportParser _importParser;

    public AppUsersController(AppDbContext context, IUserImportParser importParser)
    {
        _context = context;
        _importParser = importParser;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.AppUsers.Include(u => u.Department).ToListAsync();
        ViewBag.TotalDepartmentsCount = await _context.Departments.CountAsync();
        return View(users);
    }

    public IActionResult Create()
    {
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppUser appUser, string password)
    {
        appUser.SicilNo = Normalize(appUser.SicilNo);

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("Password", "Şifre alanı zorunludur.");
        }

        if (appUser.SicilNo != null &&
            await _context.AppUsers.AnyAsync(u => u.SicilNo == appUser.SicilNo))
        {
            ModelState.AddModelError(nameof(appUser.SicilNo), "Bu sicil no zaten kayıtlı.");
        }

        if (ModelState.IsValid)
        {
            var hasher = new PasswordHasher<AppUser>();
            appUser.PasswordHash = hasher.HashPassword(appUser, password);

            _context.AppUsers.Add(appUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", appUser.DepartmentId);
        return View(appUser);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var appUser = await _context.AppUsers.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (appUser == null)
        {
            return NotFound();
        }
        return View(appUser);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appUser = await _context.AppUsers.FindAsync(id);
        if (appUser != null)
        {
            _context.AppUsers.Remove(appUser);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var appUser = await _context.AppUsers.FindAsync(id);
        if (appUser == null)
        {
            return NotFound();
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", appUser.DepartmentId);
        return View(appUser);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AppUser appUser, string? newPassword)
    {
        if (id != appUser.Id)
        {
            return NotFound();
        }

        var existingUser = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (existingUser == null)
        {
            return NotFound();
        }

        appUser.SicilNo = Normalize(appUser.SicilNo);

        if (appUser.SicilNo != null &&
            await _context.AppUsers.AnyAsync(u => u.SicilNo == appUser.SicilNo && u.Id != id))
        {
            ModelState.AddModelError(nameof(appUser.SicilNo), "Bu sicil no başka bir kullanıcıya ait.");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            appUser.PasswordHash = existingUser.PasswordHash;
        }
        else
        {
            var hasher = new PasswordHasher<AppUser>();
            appUser.PasswordHash = hasher.HashPassword(appUser, newPassword);
        }

        if (ModelState.IsValid)
        {
            _context.Update(appUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Departments = new SelectList(_context.Departments, "Id", "Name", appUser.DepartmentId);
        return View(appUser);
    }

    // ------- Toplu İçe Aktarma -------

    [HttpGet]
    public IActionResult Import() => View(new UserImportResultViewModel());

    [HttpGet]
    public IActionResult ImportTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Ad Soyad;E-posta;Departman;Rol;Sicil No;Şifre");
        sb.AppendLine("Ahmet Yılmaz;ahmet.yilmaz@sirket.com;Üretim;Kullanıcı;10001;");
        sb.AppendLine("Ayşe Demir;ayse.demir@sirket.com;Kalite;Kalite Kontrol;10002;Gizli.123");
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "kullanici-import-sablonu.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Lütfen bir dosya seçin.");
            return View(new UserImportResultViewModel());
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".csv" or ".xlsx" or ".xlsm"))
        {
            ModelState.AddModelError("", "Yalnızca .xlsx veya .csv dosyaları desteklenir.");
            return View(new UserImportResultViewModel());
        }

        List<UserImportRow> rows;
        try
        {
            using var stream = file.OpenReadStream();
            rows = _importParser.Parse(stream, file.FileName);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Dosya okunamadı: " + ex.Message);
            return View(new UserImportResultViewModel());
        }

        var vm = new UserImportResultViewModel
        {
            HasRun = true,
            DefaultPassword = DefaultBulkPassword,
            TotalRows = rows.Count
        };

        if (rows.Count == 0)
        {
            ModelState.AddModelError("", "Dosyada işlenecek kayıt bulunamadı. İlk satır başlık, sonraki satırlar kayıt olmalı.");
            return View(vm);
        }

        var departments = await _context.Departments.ToListAsync();
        Department? ResolveDept(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var n = s.Trim();
            return departments.FirstOrDefault(d =>
                string.Equals(d.Name, n, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(d.Code) && string.Equals(d.Code, n, StringComparison.OrdinalIgnoreCase)));
        }

        var existingEmails = (await _context.AppUsers.Select(u => u.Email).ToListAsync())
            .Select(e => e.ToLowerInvariant()).ToHashSet();
        var existingSicils = (await _context.AppUsers.Where(u => u.SicilNo != null).Select(u => u.SicilNo!).ToListAsync())
            .Select(s => s.ToLowerInvariant()).ToHashSet();

        var seenEmails = new HashSet<string>();
        var seenSicils = new HashSet<string>();
        var emailValidator = new EmailAddressAttribute();
        var hasher = new PasswordHasher<AppUser>();
        var toAdd = new List<AppUser>();

        foreach (var row in rows)
        {
            var res = new UserImportRowResult
            {
                RowNumber = row.RowNumber,
                FullName = row.FullName ?? "",
                Email = row.Email ?? ""
            };

            if (string.IsNullOrWhiteSpace(row.FullName)) { res.Error = "Ad Soyad boş."; vm.Results.Add(res); continue; }
            if (string.IsNullOrWhiteSpace(row.Email)) { res.Error = "E-posta boş."; vm.Results.Add(res); continue; }

            var email = row.Email.Trim();
            if (!emailValidator.IsValid(email)) { res.Error = "Geçersiz e-posta."; vm.Results.Add(res); continue; }

            var emailKey = email.ToLowerInvariant();
            if (existingEmails.Contains(emailKey)) { res.Error = "Bu e-posta zaten sistemde kayıtlı."; vm.Results.Add(res); continue; }
            if (!seenEmails.Add(emailKey)) { res.Error = "Dosyada bu e-posta birden fazla kez var."; vm.Results.Add(res); continue; }

            var dept = ResolveDept(row.Department);
            if (dept == null)
            {
                res.Error = string.IsNullOrWhiteSpace(row.Department)
                    ? "Departman boş."
                    : $"Departman bulunamadı: {row.Department}";
                vm.Results.Add(res);
                continue;
            }

            var sicil = Normalize(row.SicilNo);
            if (sicil != null)
            {
                var sk = sicil.ToLowerInvariant();
                if (existingSicils.Contains(sk)) { res.Error = $"Sicil no zaten kayıtlı: {sicil}"; vm.Results.Add(res); continue; }
                if (!seenSicils.Add(sk)) { res.Error = $"Dosyada sicil no tekrar ediyor: {sicil}"; vm.Results.Add(res); continue; }
            }

            var user = new AppUser
            {
                FullName = row.FullName.Trim(),
                Email = email,
                DepartmentId = dept.Id,
                Role = ParseRole(row.Role),
                SicilNo = sicil
            };
            user.PasswordHash = hasher.HashPassword(user,
                string.IsNullOrWhiteSpace(row.Password) ? DefaultBulkPassword : row.Password.Trim());

            toAdd.Add(user);
            res.Success = true;
            vm.Results.Add(res);
        }

        if (toAdd.Count > 0)
        {
            _context.AppUsers.AddRange(toAdd);
            await _context.SaveChangesAsync();
        }
        vm.AddedCount = toAdd.Count;

        return View(vm);
    }

    private static string? Normalize(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static UserRole ParseRole(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return UserRole.Kullanici;
        var n = s.Trim().ToLowerInvariant()
            .Replace(" ", "").Replace("ı", "i").Replace("ş", "s")
            .Replace("ö", "o").Replace("ü", "u").Replace("ç", "c").Replace("ğ", "g");
        return n switch
        {
            "admin" or "yonetici" or "yoneticiadmin" or "administrator" => UserRole.Admin,
            "kalitekontrol" or "kalitekontrolsorumlusu" or "kalite" or "qa" => UserRole.KaliteKontrol,
            _ => UserRole.Kullanici
        };
    }
}
