using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;

namespace OmniMart.Tests.Builders;

public class UserBuilder
{
    private Guid? _userId = null;
    private string _firstName = "Target";
    private string _lastName = "User";
    private string _email = "target@test.com";
    private string _phoneNumber = "1234567890";
    private string _passwordHash = "ValidHash123!";
    private string _accountNumber = "ACC_12345678";

    private SystemRole _role = SystemRole.Customer;
    private bool _isSuspended = false;
    private string _suspensionReason = string.Empty;

    public UserBuilder WithId(Guid id)
    {
        _userId = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithRole(SystemRole role)
    {
        _role = role;
        return this;
    }
    public UserBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UserBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UserBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public UserBuilder WithAccountNumber(string accountNumber)
    {
        _accountNumber = accountNumber;
        return this;
    }
    public UserBuilder AsSuspended(string reason = "Suspended for testing")
    {
        _isSuspended = true;
        _suspensionReason = reason;
        return this;
    }

    public User Build()
    {
        var user = new User(_firstName, _lastName, _email, _phoneNumber, _passwordHash, _accountNumber, _role);

        if (_userId.HasValue)
        {
            typeof(User).GetProperty("Id")?.SetValue(user, _userId.Value);
        }

        if (_isSuspended)
        {
            user.Suspend(_suspensionReason);
        }

        return user;
    }
}