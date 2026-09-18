using FluentAssertions;
using IsoDof.Web.Controllers;
using IsoDof.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IsoDof.Tests.Controllers;

public class HomeControllerTests
{
    // Bu test, Privacy action'ının doğru çalıştığını doğrular.
    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        // 1. ARRANGE (Hazırlık)
        // HomeController iki bağımlılığa sahip: ILogger ve AppDbContext.
        // Bunları mocklayarak (taklit ederek) teste dahil ediyoruz.
        var mockLogger = new Mock<ILogger<HomeController>>();
        
        // DbContext mocklamak genelde in-memory (hafıza) veritabanı ile yapılır ama
        // şimdilik sadece null geçiyoruz çünkü Privacy metodunda veritabanı kullanılmıyor.
        var controller = new HomeController(mockLogger.Object, context: null!);

        // 2. ACT (Eylemi Gerçekleştirme)
        // Endpoint'i tetikliyoruz.
        var result = controller.Privacy();

        // 3. ASSERT (Doğrulama)
        // Dönen sonucun bir ViewResult (sayfa gösterimi) olduğunu kontrol ediyoruz.
        result.Should().BeOfType<ViewResult>();
    }
}
