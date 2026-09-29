using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(u => u.FirstName).HasMaxLength(50);
            builder.Property(u => u.LastName).HasMaxLength(50);

            builder.Property(u => u.AccountNumber).HasMaxLength(12);
            builder.HasIndex(u => u.AccountNumber).IsUnique();

            builder.Property(u => u.Email).HasMaxLength(250);
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.PhoneNumber).HasMaxLength(250);
            builder.HasIndex(u => u.PhoneNumber).IsUnique();

            builder.Property(u => u.PasswordHash).HasMaxLength(256);
            builder.HasQueryFilter(u => !u.IsDeleted);



        }
    }
}