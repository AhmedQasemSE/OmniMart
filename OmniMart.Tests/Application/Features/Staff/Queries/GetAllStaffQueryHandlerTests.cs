using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Staff.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Staff.Queries;

public class GetAllStaffQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<GetAllStaffQueryHandler>> _loggerMock = new();

    private readonly GetAllStaffQueryHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();

    public GetAllStaffQueryHandlerTests()
    {
        _handler = new GetAllStaffQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns( _defaultUserId.ToString());
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.Admin.ToString());
    }

    private void SetupStaffDb(params StaffProfile[] staff)
    {
        var mockDbSet = staff.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.StaffProfiles).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(SystemRole.Customer)]
    [InlineData(SystemRole.Vendor)]
    [InlineData(SystemRole.SupportAgent)]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAdminOrSuperAdmin(SystemRole role)
    {
        _currentUserServiceMock.Setup(s => s.Role).Returns(role.ToString());
        var query = new GetAllStaffQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedResult_WhenNoStaffExists()
    {
        SetupCurrentUser();
        SetupStaffDb(); 

        var query = new GetAllStaffQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm_WhenProvided()
    {
        SetupCurrentUser();

        var user1 = new UserBuilder().WithFirstName("Ahmed").Build();
        var staff1 = new StaffProfileBuilder().WithUser(user1).Build();

        var user2 = new UserBuilder().WithFirstName("Samir").Build();
        var staff2 = new StaffProfileBuilder().WithUser(user2).Build();

        SetupStaffDb(staff1, staff2);

        var query = new GetAllStaffQuery(SearchTerm: "ahmed");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Data.Should().ContainSingle();
        result.Value.Data.First().FullName.Should().Contain("Ahmed");
    }

    [Fact]
    public async Task Handle_ShouldFilterByDepartment_WhenProvided()
    {
        SetupCurrentUser();

        var user1 = new UserBuilder().Build();
        var staffIt = new StaffProfileBuilder().WithUser(user1).WithDepartment(Department.IT).Build();

        var user2 = new UserBuilder().Build();
        var staffHr = new StaffProfileBuilder().WithUser(user2).WithDepartment(Department.HR).Build();

        SetupStaffDb(staffIt, staffHr);

        var query = new GetAllStaffQuery(DepartmentFilter: Department.HR);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Data.Should().ContainSingle();
        result.Value.Data.First().Department.Should().Be(Department.HR.ToString());
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedAndMappedData_WhenValidRequest()
    {
        SetupCurrentUser();

        var user1 = new UserBuilder().WithFirstName("A").Build();
        var user2 = new UserBuilder().WithFirstName("B").Build();
        var user3 = new UserBuilder().WithFirstName("C").Build();

        var staff1 = new StaffProfileBuilder().WithUser(user1).Build();
        var staff2 = new StaffProfileBuilder().WithUser(user2).Build();
        var staff3 = new StaffProfileBuilder().WithUser(user3).Build();

        SetupStaffDb(staff1, staff2, staff3);

        var query = new GetAllStaffQuery(Page: 1, PageSize: 2);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.Data.Should().HaveCount(2);

        var firstDto = result.Value.Data.First();
        firstDto.FullName.Should().NotBeNullOrWhiteSpace();
        firstDto.StaffNumber.Should().StartWith("STF_");
    }

    [Theory]
    [InlineData("ahmed")]
    [InlineData("nassar")]
    [InlineData("ahmed@mail")]
    [InlineData("STF_999")]
    [InlineData("ACC_777")]
    public async Task Handle_ShouldFilterBySearchTerm_AcrossAllFields(string searchTerm)
    {
        SetupCurrentUser();

        var targetUser = new UserBuilder()
            .WithFirstName("Ahmed")
            .WithLastName("Nassar")
            .WithEmail("ahmed@mail.com")
            .WithAccountNumber("ACC_77778910")
            .Build();

        var targetStaff = new StaffProfileBuilder()
            .WithUser(targetUser)
            .WithStaffNumber("STF_999888")
            .Build();

        var noiseUser = new UserBuilder()
            .WithFirstName("Samir")
            .WithLastName("Noise")
            .WithEmail("samir@mail.com")
            .WithAccountNumber("ACC_11122234")
            .Build();

        var noiseStaff = new StaffProfileBuilder()
            .WithUser(noiseUser)
            .WithStaffNumber("STF_111222")
            .Build();

        SetupStaffDb(targetStaff, noiseStaff);

        var query = new GetAllStaffQuery(SearchTerm: searchTerm);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
    }

    #endregion
}