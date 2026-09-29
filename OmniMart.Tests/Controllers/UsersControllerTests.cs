using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Users.Commands;
using OmniMart.Controllers;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_mediatorMock.Object);
    }

    #region 1. Change Role Tests

    [Fact]
    public async Task ChangeUserRole_WhenSuccessful_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        var command = new ChangeUserRoleCommand(userId, SystemRole.Manager);

        _mediatorMock.Setup(m => m.Send(It.IsAny<ChangeUserRoleCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ChangeUserRole(userId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ChangeUserRole_WhenUserNotFound_ShouldReturnNotFound404()
    {

        var userId = Guid.NewGuid();
        var command = new ChangeUserRoleCommand(userId, SystemRole.Manager);

        _mediatorMock.Setup(m => m.Send(It.IsAny<ChangeUserRoleCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Failure("User not found.", ErrorType.NotFound));

        var result = await _controller.ChangeUserRole(userId, command, CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    #endregion

    #region 2. Suspend User Tests

    [Fact]
    public async Task SuspendUser_WhenSuccessful_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        var command = new SuspendUserCommand(userId, "Violation of platform policies");

        _mediatorMock.Setup(m => m.Send(It.IsAny<SuspendUserCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.SuspendUser(userId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region 3. Reactivate User Tests

    [Fact]
    public async Task ReactivateUser_WhenSuccessful_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ReactivateUserCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ReactivateUser(userId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion
}