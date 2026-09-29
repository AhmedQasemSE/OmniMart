using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Auth.Command;
using OmniMart.Application.Features.Auth.Commands.ForgotPassword;
using OmniMart.Application.Features.Customers.Commands;
using OmniMart.Application.Features.Vendors.Commands;
using OmniMart.Controllers;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();

        _controller = new AuthController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    #region 1. Customer Registration Tests

    [Fact]
    public async Task RegisterCustomerAsync_WhenSuccessful_ShouldReturnOk()
    {
        var command = new RegisterCustomerCommand("Ali", "Ahmad", "ali@test.com", "0500000000", "Pass123!");
        var expectedId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterCustomerCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedId));

        var result = await _controller.RegisterCustomerAsync(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task RegisterCustomerAsync_WhenValidationFails_ShouldReturnUnprocessableEntity422()
    {
        var command = new RegisterCustomerCommand("", "", "invalid-email", "", "");

        _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterCustomerCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Failure("Validation Error", ErrorType.Validation));

        var result = await _controller.RegisterCustomerAsync(command, CancellationToken.None);

        var unprocessableResult = result as UnprocessableEntityObjectResult;
        unprocessableResult.Should().NotBeNull();
        unprocessableResult!.StatusCode.Should().Be(422);
    }

    #endregion

    #region 2. Vendor Registration Tests

    [Fact]
    public async Task RegisterVendorAsync_WhenSuccessful_ShouldReturnOk()
    {
        var command = new RegisterVendorCommand("Sami", "Tech", "sami@tech.com", "0555555555", "Pass123!", "Sami Store", "1234567890");
        var expectedId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterVendorCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedId));

        var result = await _controller.RegisterVendorAsync(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region 3. Login Tests

    [Fact]
    public async Task LoginAsync_WhenSuccessful_ShouldReturnOkWithToken()
    {
        var command = new LoginCommand("user@test.com", "Pass123!");
        var expectedResponse = new AuthResponse("John Doe", Guid.NewGuid(), "eyJhbGciOi...", "refresh-token", SystemRole.Customer);

        _mediatorMock.Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<AuthResponse>.Success(expectedResponse));

        var result = await _controller.LoginAsync(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsInvalid_ShouldReturnBadRequest400()
    {
        var command = new LoginCommand("user@test.com", "WrongPass!");

        _mediatorMock.Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<AuthResponse>.Failure("Invalid email or password.", ErrorType.Failure));

        var result = await _controller.LoginAsync(command, CancellationToken.None);

        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    #endregion

    #region 4. Password Management Tests

    [Fact]
    public async Task ForgotPassword_WhenSuccessful_ShouldReturnOk()
    {
        var command = new ForgotPasswordCommand("user@test.com");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ForgotPassword(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ResetPassword_WhenSuccessful_ShouldReturnOk()
    {
        var command = new ResetPasswordCommand("user@test.com", "123456", "NewPass123!", "NewPass123!");

        _mediatorMock.Setup(m => m.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ResetPassword(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region 5. Refresh Token Tests

    [Fact]
    public async Task RefreshToken_WhenCookieExists_ShouldReturnOkWithNewTokens()
    {
        _controller.ControllerContext.HttpContext.Request.Headers["Cookie"] = "RefreshToken=valid-secret-token";

        var expectedResponse = new AuthResponse("John Doe", Guid.NewGuid(), "new-jwt-token", "new-refresh-token", SystemRole.Customer);

        _mediatorMock.Setup(m => m.Send(It.Is<RefreshTokenCommand>(c => c.RefreshToken == "valid-secret-token"), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<AuthResponse>.Success(expectedResponse));

        var result = await _controller.RefreshToken(CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RefreshToken_WhenCookieIsMissing_ShouldReturnUnauthorized()
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Remove("Cookie");

        var result = await _controller.RefreshToken(CancellationToken.None);

        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    #endregion
}