using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class StaffProfileBuilder
{
    private Guid? _id = null;
    private Guid _userId = Guid.NewGuid();
    private Department _departmentRole = Department.IT;
    private string _staffNumber = $"STF_{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
    private decimal _salary = 5000m;

    private User? _customUser = null;

    public StaffProfileBuilder WithId(Guid id) { _id = id; return this; }
    public StaffProfileBuilder WithUserId(Guid userId) { _userId = userId; return this; }
    public StaffProfileBuilder WithDepartment(Department department) { _departmentRole = department; return this; }
    public StaffProfileBuilder WithStaffNumber(string staffNumber) { _staffNumber = staffNumber; return this; }
    public StaffProfileBuilder WithSalary(decimal salary) { _salary = salary; return this; }

    public StaffProfileBuilder WithUser(User user)
    {
        _customUser = user;
        _userId = user.Id;
        return this;
    }

    public StaffProfile Build()
    {
        var profile = new StaffProfile(_userId, _staffNumber, _departmentRole, _salary);

        if (_id.HasValue)
        {
            SetPrivateProperty(profile, "Id", _id.Value);
        }

        if (_customUser != null)
        {
            SetPrivateProperty(profile, "User", _customUser);
        }

        return profile;
    }

    private void SetPrivateProperty(object instance, string propertyName, object value)
    {
        var type = instance.GetType();
        PropertyInfo? property = null;

        while (type != null && property == null)
        {
            property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }

        property?.SetValue(instance, value);
    }
}