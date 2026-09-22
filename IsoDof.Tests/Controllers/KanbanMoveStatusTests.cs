using System.Security.Claims;
using FluentAssertions;
using IsoDof.Web.Controllers;
using IsoDof.Web.Data;
using IsoDof.Web.Models.Entities;
using IsoDof.Web.Models.Entities.Enums;
using IsoDof.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IsoDof.Tests.Controllers;

public class KanbanMoveStatusTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DofsController CreateController(AppDbContext context, int userId, string role = "Kullanici")
    {
        var controller = new DofsController(context, Mock.Of<IEmailService>(), Mock.Of<IDofReportService>(),
            Mock.Of<INotificationService>(), Mock.Of<IFileStorageService>());
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
        }, "Test"));
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("test_url");
        controller.Url = url.Object;
        return controller;
    }

    private static async Task<AppDbContext> SeedAsync(DofStatus status, bool withOpenAction = false)
    {
        var context = NewContext();
        var dof = new Dof
        {
            Id = 1, Title = "Kanban testi", Description = "x", DepartmentId = 1,
            CreatedByUserId = 5, AssignedToUserId = 6, Status = status, DueDate = DateTime.UtcNow.AddDays(7)
        };
        context.Dofs.Add(dof);
        if (withOpenAction)
        {
            context.DofActions.Add(new DofAction { Id = 1, DofId = 1, Description = "Açık faaliyet" });
        }
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task MoveStatus_AllowedTransition_UpdatesStatusAndWritesHistory()
    {
        await using var context = await SeedAsync(DofStatus.Acik);
        var controller = CreateController(context, userId: 5);

        var result = await controller.MoveStatus(1, DofStatus.Incelemede);

        result.Should().BeOfType<OkObjectResult>();
        (await context.Dofs.SingleAsync()).Status.Should().Be(DofStatus.Incelemede);
        var history = await context.DofStatusHistories.SingleAsync();
        history.OldStatus.Should().Be(DofStatus.Acik);
        history.NewStatus.Should().Be(DofStatus.Incelemede);
        history.ChangedByUserId.Should().Be(5);
    }

    [Fact]
    public async Task MoveStatus_DisallowedTransition_ReturnsBadRequestAndKeepsStatus()
    {
        await using var context = await SeedAsync(DofStatus.Acik);
        var controller = CreateController(context, userId: 5);

        var result = await controller.MoveStatus(1, DofStatus.Kapatildi);

        result.Should().BeOfType<BadRequestObjectResult>();
        (await context.Dofs.SingleAsync()).Status.Should().Be(DofStatus.Acik);
        context.DofStatusHistories.Should().BeEmpty();
    }

    [Fact]
    public async Task MoveStatus_ClosingWithOpenActions_IsRejected()
    {
        await using var context = await SeedAsync(DofStatus.FaaliyetPlanlandi, withOpenAction: true);
        var controller = CreateController(context, userId: 5);

        var result = await controller.MoveStatus(1, DofStatus.Kapatildi);

        result.Should().BeOfType<BadRequestObjectResult>();
        (await context.Dofs.SingleAsync()).Status.Should().Be(DofStatus.FaaliyetPlanlandi);
    }

    [Fact]
    public async Task MoveStatus_ForeignDof_IsForbidden()
    {
        await using var context = await SeedAsync(DofStatus.Acik);
        var controller = CreateController(context, userId: 99);

        var result = await controller.MoveStatus(1, DofStatus.Incelemede);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        (await context.Dofs.SingleAsync()).Status.Should().Be(DofStatus.Acik);
    }
}
