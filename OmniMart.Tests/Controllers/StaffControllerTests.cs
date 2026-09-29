using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Staff.Commands;
using OmniMart.Application.Features.Staff.Queries;
using OmniMart.Controllers;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class StaffControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly StaffController _controller;

    public StaffControllerTests()
    {
        _controller = new StaffController(_mediatorMock.Object);
    }

    #region 1. Create Staff Tests

    [Fact]
    public async Task CreateStaffAsync_WhenSuccessful_ShouldReturnOk()
    {
        var command = new RegisterStaffCommand("Omar", "Ali", "omar@test.com", "0500000000", "Pass123!", Department.HR, SystemRole.Manager, 5000m);
        var expectedId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterStaffCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedId));

        var result = await _controller.CreateStaffAsync(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateStaffAsync_WhenValidationFails_ShouldReturnUnprocessableEntity422()
    {
        var command = new RegisterStaffCommand("", "", "", "", "", Department.IT, SystemRole.Admin, 0);

        _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterStaffCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Failure("Validation Error", ErrorType.Validation));

        var result = await _controller.CreateStaffAsync(command, CancellationToken.None);

        var unprocessableResult = result as UnprocessableEntityObjectResult;
        unprocessableResult.Should().NotBeNull();
        unprocessableResult!.StatusCode.Should().Be(422);
    }

    #endregion

    #region 2. Query Tests

    [Fact]
    public async Task GetAllStaff_WhenSuccessful_ShouldReturnOk()
    {
        var query = new GetAllStaffQuery(null, null, 1, 10);
        var expectedResult = new PaginatedResult<StaffSummaryDto>(new(), 0, 1, 10);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllStaffQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<PaginatedResult<StaffSummaryDto>>.Success(expectedResult));

        var result = await _controller.GetAllStaff(query, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    #endregion

    #region 3. Actions Tests

    [Fact]
    public async Task IncreaseSalary_WhenSuccessful_ShouldReturnOk()
    {
        var staffId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<IncreaseStaffSalaryCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.IncreaseSalary(staffId, 1000m, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task TransferDepartment_WhenSuccessful_ShouldReturnOk()
    {
        var staffId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<TransferStaffDepartmentCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.TransferDepartment(staffId, Department.IT, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion
}