using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Vendors.Commands;
using OmniMart.Application.Features.Vendors.Queries;
using OmniMart.Controllers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class VendorControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly VendorController _controller;

    public VendorControllerTests()
    {
        _controller = new VendorController(_mediatorMock.Object);
    }

    #region 1. Vendor Profile Tests

    [Fact]
    public async Task GetVendorProfile_WhenSuccessful_ShouldReturnOk()
    {
        var expectedProfile = new VendorProfileDto(Guid.NewGuid(), "Tech Store", "VND_123", "CR_999", 1500m, 5m, true, new byte[] { 1 });

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetVendorProfileQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<VendorProfileDto>.Success(expectedProfile));

        var result = await _controller.GetVendorProfile(CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedProfile);
    }

    [Fact]
    public async Task GetVendorProfile_WhenNotFound_ShouldReturnNotFound404()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetVendorProfileQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<VendorProfileDto>.Failure("Vendor profile not found.", ErrorType.NotFound));

        var result = await _controller.GetVendorProfile(CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    #endregion

    #region 2. Admin Actions Tests

    [Fact]
    public async Task ApproveVendor_WhenSuccessful_ShouldReturnOk()
    {
        var vendorId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ApproveVendorCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ApproveVendor(vendorId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion
}