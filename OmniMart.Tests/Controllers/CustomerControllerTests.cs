using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.CustomerProfiles.Commands;
using OmniMart.Application.Features.Customers.Commands;
using OmniMart.Application.Features.Customers.Queries;
using OmniMart.Controllers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class CustomerControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly CustomerController _controller;

    public CustomerControllerTests()
    {
        _controller = new CustomerController(_mediatorMock.Object);
    }

    #region 1. Profile & Points Tests

    [Fact]
    public async Task GetCustomerProfile_WhenSuccessful_ShouldReturnOk()
    {
        var expectedProfile = new CustomerProfileDto(Guid.NewGuid(), "Ali", "Ahmad", "ali@test.com", "0500", "ACC_123", 100, 500m);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCustomerProfileQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<CustomerProfileDto>.Success(expectedProfile));

        var result = await _controller.GetCustomerProfile(CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedProfile);
    }

    [Fact]
    public async Task GetCustomerProfile_WhenNotFound_ShouldReturnNotFound404()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCustomerProfileQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<CustomerProfileDto>.Failure("Profile not found.", ErrorType.NotFound));

        var result = await _controller.GetCustomerProfile(CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task RedeemLoyaltyPoints_WhenSuccessful_ShouldReturnOk()
    {
        var command = new RedeemLoyaltyPointsCommand(50);

        _mediatorMock.Setup(m => m.Send(It.IsAny<RedeemLoyaltyPointsCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.RedeemLoyaltyPoints(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region 2. Address Management Tests

    [Fact]
    public async Task AddCustomerAddress_WhenSuccessful_ShouldReturnOk()
    {
        var command = new AddCustomerAddressCommand("Home", "Riyadh", "King Fahd St", "12345", "0500000000");
        var expectedAddressId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<AddCustomerAddressCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedAddressId));

        var result = await _controller.AddCustomerAddress(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedAddressId);
    }

    [Fact]
    public async Task GetCustomerAddresses_WhenSuccessful_ShouldReturnOk()
    {
        var expectedAddresses = new List<CustomerAddressDto>
        {
            new CustomerAddressDto(Guid.NewGuid(), "Home", "Riyadh", "Street 1", "123", "050")
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCustomerAddressesQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<List<CustomerAddressDto>>.Success(expectedAddresses));

        var result = await _controller.GetCustomerAddresses(CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedAddresses);
    }

    [Fact]
    public async Task UpdateCustomerAddress_WhenSuccessful_ShouldReturnOk()
    {
        var addressId = Guid.NewGuid();
        var command = new UpdateCustomerAddressCommand(addressId, "Work", "Jeddah", "Street 2", "54321", "0511111111");

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateCustomerAddressCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UpdateCustomerAddress(addressId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteCustomerAddress_WhenSuccessful_ShouldReturnOk()
    {
        var addressId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteCustomerAddressCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeleteCustomerAddress(addressId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion
}