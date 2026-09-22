using System.Security.Claims;
using FluentAssertions;
using IsoDof.Web.Controllers;
using IsoDof.Web.Data;
using IsoDof.Web.Models;
using IsoDof.Web.Models.Entities;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IsoDof.Tests.Security;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Guclu123", true)]
    [InlineData("abcdefg1", true)]
    [InlineData("kisa1", false)]          // 8 karakterden kısa
    [InlineData("sadeceharf", false)]     // rakam yok
    [InlineData("12345678", false)]       // harf yok
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_EnforcesLengthLetterAndDigit(string? password, bool expected)
    {
        PasswordPolicy.IsValid(password, out var error).Should().Be(expected);
        (error == null).Should().Be(expected);
    }
}

public class DofAccessTests
{
    private static ClaimsPrincipal UserWithRole(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));

    private static readonly Dof Dof = new() { Id = 1, DepartmentId = 10, CreatedByUserId = 5, AssignedToUserId = 6 };

    [Fact]
    public void Admin_CanAccessAnyDof() =>
        DofAccess.CanAccess(UserWithRole("Admin"), Dof, 99, null).Should().BeTrue();

    [Fact]
    public void QualityControl_CanAccessOwnDepartmentOnly()
    {
        DofAccess.CanAccess(UserWithRole("KaliteKontrol"), Dof, 99, 10).Should().BeTrue();
        DofAccess.CanAccess(UserWithRole("KaliteKontrol"), Dof, 99, 11).Should().BeFalse();
    }

    [Fact]
    public void QualityControl_WithoutDepartment_CannotAccess() =>
        DofAccess.CanAccess(UserWithRole("KaliteKontrol"), Dof, 99, null).Should().BeFalse();

    [Fact]
    public void RegularUser_CanAccessOnlyCreatedOrAssigned()
    {
        DofAccess.CanAccess(UserWithRole("Kullanici"), Dof, 5, 10).Should().BeTrue();  // açan
        DofAccess.CanAccess(UserWithRole("Kullanici"), Dof, 6, 10).Should().BeTrue();  // atanan
        DofAccess.CanAccess(UserWithRole("Kullanici"), Dof, 7, 10).Should().BeFalse(); // aynı departman, ilgisiz kullanıcı
    }
}

public class DofsControllerAuthorizationTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DofsController CreateController(AppDbContext context, int userId, Mock<IFileStorageService> fileStorage)
    {
        var controller = new DofsController(context, Mock.Of<IEmailService>(), Mock.Of<IDofReportService>(),
            Mock.Of<INotificationService>(), fileStorage.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Kullanici"),
        }, "Test"));
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("test_url");
        controller.Url = url.Object;
        return controller;
    }

    private static async Task<AppDbContext> SeedForeignDofAsync()
    {
        var context = NewContext();
        context.Dofs.Add(new Dof { Id = 1, Title = "Başkasının DÖF'ü", Description = "x", DepartmentId = 1, CreatedByUserId = 2, AssignedToUserId = 3, DueDate = DateTime.UtcNow.AddDays(7) });
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task AddComment_OnForeignDof_IsDenied()
    {
        await using var context = await SeedForeignDofAsync();
        var controller = CreateController(context, userId: 99, new Mock<IFileStorageService>());

        var result = await controller.AddComment(1, "yetkisiz yorum");

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AccessDenied");
        context.DofComments.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAttachment_OnForeignDof_IsDeniedBeforeSavingFile()
    {
        await using var context = await SeedForeignDofAsync();
        var storage = new Mock<IFileStorageService>();
        var controller = CreateController(context, userId: 99, storage);

        var result = await controller.UploadAttachment(1, Mock.Of<IFormFile>());

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AccessDenied");
        storage.Verify(s => s.SaveAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Attachment_OnForeignDof_IsDenied()
    {
        await using var context = await SeedForeignDofAsync();
        context.DofAttachments.Add(new DofAttachment { Id = 7, DofId = 1, FileName = "rapor.pdf", StoredFileName = "abc.pdf", UploadedByUserId = 2 });
        await context.SaveChangesAsync();
        var storage = new Mock<IFileStorageService>();
        var controller = CreateController(context, userId: 99, storage);

        var result = await controller.Attachment(7);

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AccessDenied");
        storage.Verify(s => s.GetPath(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

public class AccountLockoutTests
{
    [Fact]
    public async Task Login_LocksAccountAfterMaxFailedAttempts()
    {
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new AppUser { Id = 1, FullName = "Test Kullanıcı", Email = "test@sirket.com", DepartmentId = 1 };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, "DogruSifre1");
        context.Departments.Add(new Department { Id = 1, Name = "Kalite" });
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();

        var controller = new AccountController(context, NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        for (var i = 0; i < AccountController.MaxFailedAttempts; i++)
        {
            controller.ModelState.Clear(); // her HTTP isteğinde yeni controller oluşur
            await controller.Login(new LoginViewModel { Email = user.Email, Password = "YanlisSifre1" });
        }

        var saved = await context.AppUsers.SingleAsync();
        saved.LockoutEndUtc.Should().NotBeNull();
        saved.LockoutEndUtc!.Value.Should().BeAfter(DateTime.UtcNow);

        // Kilitliyken doğru şifre de kabul edilmez.
        controller.ModelState.Clear();
        var result = await controller.Login(new LoginViewModel { Email = user.Email, Password = "DogruSifre1" });
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }
}

public class FileStorageServiceTests
{
    [Fact]
    public void GetPath_IgnoresDirectoryTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "isodof-test-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "dof-attachments"));
        File.WriteAllText(Path.Combine(root, "gizli.txt"), "gizli");
        try
        {
            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(root);
            env.SetupGet(e => e.WebRootPath).Returns(Path.Combine(root, "wwwroot"));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:RootPath"] = root })
                .Build();
            var service = new FileStorageService(env.Object, config);

            service.GetPath("dof-attachments", "../gizli.txt").Should().BeNull();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
