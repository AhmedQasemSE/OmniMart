using FluentAssertions;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using Xunit;

namespace OmniMart.Tests.Domain.Entities;

public class UserTests
{
    private User CreateTestUser(
        string firstName = "Ahmed",
        string lastName = "Salem",
        string email = "test@omnimart.com",
        string phone = "+905555555555",
        string passwordHash = "hashed_pwd_123",
        string accountNumber = "123456789012",
        SystemRole role = SystemRole.Customer)
    {
        return new User(firstName, lastName, email, phone, passwordHash, accountNumber, role);
    }

    #region Constructor & Validation Tests

    [Fact]
    public void Constructor_ShouldCreateUser_WhenAllDataIsValid()
    {
        var user = CreateTestUser();

        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.FirstName.Should().Be("Ahmed");
        user.IsActive.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
        user.IsEmailConfirmed.Should().BeFalse();
        user.Role.Should().Be(SystemRole.Customer);
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("", "Salem")]
    [InlineData("Ahmed", "")]
    [InlineData("Ahmed123", "Salem")]
    [InlineData("Ahmed", "Salem45")]
    public void Constructor_ShouldThrowException_WhenNameIsInvalid(string invalidFirstName, string invalidLastName)
    {
        Action act = () => CreateTestUser(firstName: invalidFirstName, lastName: invalidLastName);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowException_WhenEmailIsEmpty(string? emptyEmail)
    {
        Action act = () => CreateTestUser(email: emptyEmail!);

        act.Should().Throw<ArgumentException>().WithMessage("*Email cannot be empty.*");
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@missingusername.com")]
    [InlineData("username@.com")]
    [InlineData("username@domain..com")]
    public void Constructor_ShouldThrowException_WhenEmailFormatIsInvalid(string invalidEmail)
    {
        Action act = () => CreateTestUser(email: invalidEmail);

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid email format*");
    }

    [Theory]
    [InlineData("12345")] 
    [InlineData("abcdef")] 
    [InlineData("+123456abc")] 
    public void Constructor_ShouldThrowException_WhenPhoneIsInvalid(string invalidPhone)
    {
        Action act = () => CreateTestUser(phone: invalidPhone);

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid phone number format*");
    }

    [Fact]
    public void Constructor_ShouldAllowEmptyPhoneNumber()
    {
        var user = CreateTestUser(phone: "");

        user.Should().NotBeNull();
        user.PhoneNumber.Should().Be("");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678901")] 
    [InlineData("1234567890123")] 
    public void Constructor_ShouldThrowException_WhenAccountNumberIsInvalid(string invalidAccount)
    {
        Action act = () => CreateTestUser(accountNumber: invalidAccount);

        act.Should().Throw<ArgumentException>().WithMessage("*AccountNumber must be exactly 12 characters*");
    }

    #endregion

    #region Lifecycle & State Management Tests

    [Fact]
    public void Suspend_ShouldDeactivateUserAndSetReason_WhenReasonIsValid()
    {
        var user = CreateTestUser();
        var reason = "Violation of terms";

        user.Suspend(reason);

        user.IsActive.Should().BeFalse();
        user.SuspensionReason.Should().Be(reason);
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Suspend_ShouldThrowException_WhenReasonIsNullOrWhiteSpace(string? invalidReason)
    {
        var user = CreateTestUser();

        Action act = () => user.Suspend(invalidReason!);

        act.Should().Throw<ArgumentException>().WithMessage("*Suspension reason must be provided*");
    }

    [Fact]
    public void Reactivate_ShouldActivateUserAndClearReason()
    {
        var user = CreateTestUser();
        user.Suspend("Temporary Ban");

        user.Reactivate();

        user.IsActive.Should().BeTrue();
        user.SuspensionReason.Should().BeNull();
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldUpdateLastLoginAt()
    {
        var user = CreateTestUser();

        user.RecordSuccessfulLogin();

        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedToTrueAndDeactivate()
    {
        var user = CreateTestUser();

        user.SoftDelete();

        user.IsDeleted.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion
    #region Updates & Tokens Tests

    [Fact]
    public void SetPasswordResetToken_ShouldUpdateTokenAndExpiration()
    {
        var user = CreateTestUser();
        var token = "reset_token_123_abc";
        var expiration = DateTimeOffset.UtcNow.AddHours(1); 

        user.SetPasswordResetToken(token, expiration);

        user.PasswordResetToken.Should().Be(token);
        user.ResetTokenExpiresAt.Should().Be(expiration);
    }

    [Fact]
    public void ClearPasswordResetToken_ShouldSetTokenAndExpirationToNull()
    {
        var user = CreateTestUser();

        user.SetPasswordResetToken("old_token", DateTimeOffset.UtcNow.AddHours(1));

        user.ClearPasswordResetToken();

        user.PasswordResetToken.Should().BeNull();
        user.ResetTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void UpdatePassword_ShouldChangePasswordHashAndUpdateTimestamp_WhenValid()
    {
        var user = CreateTestUser();
        var newPasswordHash = "new_hashed_password_890";

        user.UpdatePassword(newPasswordHash);

        user.PasswordHash.Should().Be(newPasswordHash);
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdatePassword_ShouldThrowException_WhenPasswordHashIsInvalid(string? invalidHash)
    {
        var user = CreateTestUser();

        Action act = () => user.UpdatePassword(invalidHash!);

        act.Should().Throw<ArgumentException>().WithMessage("*Password hash cannot be empty.*");
    }

    [Fact]
    public void UpdateEmail_ShouldChangeEmailAndUpdateTimestamp_WhenEmailIsValid()
    {
        var user = CreateTestUser();
        var newEmail = "new_valid_email@omnimart.com";

        user.UpdateEmail(newEmail);

        user.Email.Should().Be(newEmail);
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateEmail_ShouldThrowException_WhenEmailIsInvalid()
    {
        var user = CreateTestUser();
        var invalidEmail = "invalid-email..com"; 

        Action act = () => user.UpdateEmail(invalidEmail);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangeRole_ShouldUpdateRoleAndTimestamp()
    {
        var user = CreateTestUser(role: SystemRole.Customer);

        var newRole = SystemRole.Admin;

        user.ChangeRole(newRole);

        user.Role.Should().Be(newRole);
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion
}