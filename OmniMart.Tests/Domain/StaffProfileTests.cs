using FluentAssertions;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using Xunit;

namespace OmniMart.Tests.Domain.Entities;

public class StaffProfileTests
{
    private StaffProfile CreateValidStaff(Department department = Department.IT, decimal salary = 5000m)
    {
        return new StaffProfile(Guid.NewGuid(), "STF_123456", department, salary);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateStaffProfile_WhenDataIsValid()
    {
        var userId = Guid.NewGuid();
        var staff = new StaffProfile(userId, "STF_123", Department.IT, 5000m);

        staff.Id.Should().NotBeEmpty();
        staff.UserId.Should().Be(userId);
        staff.DepartmentRole.Should().Be(Department.IT);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenUserIdIsEmpty()
    {
        Action act = () => new StaffProfile(Guid.Empty, "STF_123", Department.IT, 5000m);
        act.Should().Throw<ArgumentException>().WithMessage("*UserId cannot be empty*");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenSalaryIsNegative()
    {
        Action act = () => new StaffProfile(Guid.NewGuid(), "STF_123", Department.IT, -100m);
        act.Should().Throw<ArgumentException>().WithMessage("*Salary cannot be negative*");
    }

    #endregion

    #region IncreaseSalary Tests

    [Fact]
    public void IncreaseSalary_ShouldIncreaseSalary_WhenAmountIsPositive()
    {
        var staff = CreateValidStaff(salary: 5000m);

        staff.IncreaseSalary(1500m);

        staff.Salary.Should().Be(6500m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void IncreaseSalary_ShouldThrowArgumentException_WhenAmountIsZeroOrNegative(decimal invalidAmount)
    {
        var staff = CreateValidStaff();

        Action act = () => staff.IncreaseSalary(invalidAmount);

        act.Should().Throw<ArgumentException>().WithMessage("*Amount to increase salary must be positive*");
    }

    #endregion

    #region TransferDepartment Tests

    [Fact]
    public void TransferDepartment_ShouldChangeDepartment_WhenNewDepartmentIsDifferent()
    {
        var staff = CreateValidStaff(Department.IT);

        staff.TransferDepartment(Department.HR);

        staff.DepartmentRole.Should().Be(Department.HR);
    }

    [Fact]
    public void TransferDepartment_ShouldThrowInvalidOperationException_WhenNewDepartmentIsSameAsCurrent()
    {
        var staff = CreateValidStaff(Department.HR);

        Action act = () => staff.TransferDepartment(Department.HR);

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}