using OmniMart.Domain.Enums;
using System.Data;

namespace OmniMart.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string AccountNumber { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public string? PhoneNumber { get; private set; }
        public string PasswordHash { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDeleted { get; private set; }
        public bool IsEmailConfirmed { get; private set; }
        public string? RefreshToken { get; private set; }
        public DateTimeOffset? RefreshTokenExpiryTime { get; private set; }
        public SystemRole Role { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }
        public DateTimeOffset? LastLoginAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public virtual CustomerProfile? CustomerProfile { get; private set; }
        public virtual VendorProfile? VendorProfile { get; private set; }
        public virtual StaffProfile ? StaffProfile {  get; private set; }

        public User(string firstName, string lastName, string email,string phoneNumber,string passwordHash, string accountNumber, SystemRole role) {
        ValidateName(firstName);
        ValidateName(lastName);
        ValidateAccountNumber(accountNumber);
        ValidateEmail(email);
        ValidatePhoneNumber(phoneNumber);
            Id= Guid.NewGuid();
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            PasswordHash = passwordHash;
            AccountNumber = accountNumber;
            Role = role;
            IsActive = true;        
            IsDeleted = false;       
            IsEmailConfirmed = false;
            CreatedAt = DateTimeOffset.UtcNow;


        }
#pragma warning disable CS8618
        protected User() { }
#pragma warning restore CS8618

        #region Validation Methods
        private void ValidateName(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Name cannot be empty.", nameof(name));
            }
            if (name.Any(char.IsDigit)) {
                throw new ArgumentException($"Name cannot contain numbers. Invalid: '{name}'", nameof(name));
            }
        }
        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.", nameof(email));

            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(email);

                if (mailAddress.Address != email || !email.Contains("."))
                {
                    throw new ArgumentException($"Invalid email format: '{email}'", nameof(email));
                }
            }
            catch
            {
                throw new ArgumentException($"Invalid email format: '{email}'", nameof(email));
            }
        }
        private void ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return;
            var phoneRegex = new System.Text.RegularExpressions.Regex(@"^\+?[0-9]{7,15}$");

            if (!phoneRegex.IsMatch(phoneNumber))
            {
                throw new ArgumentException($"Invalid phone number format: '{phoneNumber}'", nameof(phoneNumber));
            }

        }
        private void ValidateAccountNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number) || number.Length != 12)
            {
                throw new ArgumentException($"AccountNumber must be exactly 12 characters. Invalid: '{number}'", nameof(number));
            }
        }


        #endregion

        #region Login and Update Methods

        public void UpdateRefreshToken(string token, DateTimeOffset expiryTime)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Refresh Token cannot be empty.");

            RefreshToken = token;
            RefreshTokenExpiryTime = expiryTime;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void RevokeRefreshToken()
        {
            RefreshToken = null;
            RefreshTokenExpiryTime = null;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public string? SuspensionReason { get; private set; }

        public void Suspend(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Suspension reason must be provided.");

            IsActive = false;
            SuspensionReason = reason;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Reactivate()
        {
            IsActive = true;
            SuspensionReason = null;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public string? PasswordResetToken { get; private set; }
        public DateTimeOffset? ResetTokenExpiresAt { get; private set; }

        public void SetPasswordResetToken(string token, DateTimeOffset expiresAt)
        {
            PasswordResetToken = token;
            ResetTokenExpiresAt = expiresAt;
        }

        public void ClearPasswordResetToken()
        {
            PasswordResetToken = null;
            ResetTokenExpiresAt = null;
        }

        public void RecordSuccessfulLogin()
        {
            LastLoginAt = DateTimeOffset.UtcNow;
        }

        public void UpdatePassword(string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new ArgumentException("Password hash cannot be empty.");
            PasswordHash = newPasswordHash;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void UpdateEmail(string newEmail)
        {
            ValidateEmail(newEmail);
            Email = newEmail;
            UpdatedAt = DateTimeOffset.UtcNow; 
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            IsActive = false;
            UpdatedAt = DateTimeOffset.UtcNow; 
        }

        public void ChangeRole(SystemRole newRole)
        {
            Role = newRole;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
        #endregion
    }
}
