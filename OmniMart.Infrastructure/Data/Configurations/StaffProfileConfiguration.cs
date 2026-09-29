using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniMart.Domain.Entities;

namespace OmniMart.Infrastructure.Data.Configurations
{
    public class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
    {
        public void Configure(EntityTypeBuilder<StaffProfile> builder)
        {
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(s => s.Salary).HasColumnType("decimal(18,2)");
            builder.Property(u => u.StaffNumber).HasMaxLength(12);
            builder.HasIndex(u => u.StaffNumber).IsUnique();


            builder.HasOne(u => u.User)
           .WithOne(s => s.StaffProfile)
           .HasForeignKey<StaffProfile>(u => u.UserId)
           .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(s => !s.User!.IsDeleted);
        }


    }
}