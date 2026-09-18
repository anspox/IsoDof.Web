using FluentAssertions;
using IsoDof.Web.Controllers;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace IsoDof.Tests.Controllers;

public class DofsControllerTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private DofsController CreateControllerWithUser(AppDbContext context, string role = "Kullanici")
    {
        var mockEmail = new Mock<IEmailService>();
        var mockReport = new Mock<IDofReportService>();
        var mockNotification = new Mock<INotificationService>();
        var mockFileStorage = new Mock<IFileStorageService>();

        var controller = new DofsController(context, mockEmail.Object, mockReport.Object, mockNotification.Object, mockFileStorage.Object);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // TempData dictionary mock/setup for tests
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var mockUrlHelper = new Mock<Microsoft.AspNetCore.Mvc.IUrlHelper>();
        mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("test_url");
        controller.Url = mockUrlHelper.Object;

        return controller;
    }

    [Fact]
    public async Task Create_Get_WhenAdminOrKaliteKontrol_RedirectsToIndex()
    {
        // ARRANGE
        using var context = GetInMemoryDbContext();
        var controller = CreateControllerWithUser(context, role: "Admin");

        // ACT
        var result = await controller.Create();

        // ASSERT
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");

        controller.TempData["ErrorMessage"].Should().NotBeNull();
        controller.TempData["ErrorMessage"]!.ToString().Should().Contain("Yöneticiler ve Kalite Kontrol sorumluları DÖF açamaz");
    }

    [Fact]
    public async Task Create_Get_WhenNormalUser_ReturnsViewResultWithViewBags()
    {
        // ARRANGE
        using var context = GetInMemoryDbContext();
        var controller = CreateControllerWithUser(context, role: "Kullanici");

        // ACT
        var result = await controller.Create();

        // ASSERT
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        
        Assert.NotNull(controller.ViewBag.Departments);
        Assert.NotNull(controller.ViewBag.Users);
        Assert.NotNull(controller.ViewBag.Templates);
    }

    [Fact]
    public async Task DofSilme_Sayfasina_Gidilirse_Dof_Silinmis_Sayilmalidir()
    {
        // 1. HAZIRLIK: Her şey hazır, sana verdiğim şu sihirli kodu kopyala-yapıştır yap
        using var context = GetInMemoryDbContext();
        var controller = CreateControllerWithUser(context);

        // Veritabanına test etmek için rastgele bir DÖF ekleyelim
        context.Dofs.Add(new Dof { Id = 5, IsArchived = false, Title = "Test", Description = "Test", AssignedToUserId = 1, CreatedByUserId = 1, DueDate = DateTime.Now, Status = IsoDof.Web.Models.Entities.Enums.DofStatus.Acik });
        context.SaveChanges();

        // 2. HAREKET: Controller'daki 'ArchiveConfirmed' (Silme) metodunu 5 numarasıyla çağır.
        // (Yani bir kullanıcı sitede "Sil" butonuna basmış gibi yapıyoruz)
        await controller.ArchiveConfirmed(5, "Gereksiz açılmış");

        // 3. KONTROL EDELİM (En zevkli kısım): 5 numaralı DÖF gerçekten silinmiş mi (Arşivlenmiş mi)?
        var silinenDof = context.Dofs.Find(5);
        
        // FluentAssertions dediğimiz yapı bunu İngilizce cümle gibi yazmanı sağlar:
        silinenDof.IsArchived.Should().BeTrue(); // "Silinen dofun IsArchived değeri Doğru (True) OLMALIDIR"
    }
}




