using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BonusTest.Domain.Entities;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class BonusConfiguration : IEntityTypeConfiguration<Bonus>
{
    public void Configure(EntityTypeBuilder<Bonus> builder)
    {
        builder.ToTable("Bonus").HasKey(x => x.BonusId);
        builder.Property(x => x.BonusValue).HasPrecision(16, 2);
        builder.Property(x => x.GrossAmount).HasPrecision(16, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(16, 2);
        builder.Property(x => x.NetAmount).HasPrecision(16, 2);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => new { x.DepartmentCode, x.EmployeeNumber });

        builder.HasOne(x => x.BonusType)
            .WithMany()
            .HasForeignKey(x => x.BonusTypeId);

        builder.HasOne(x => x.Reason)
            .WithMany()
            .HasForeignKey(x => x.ReasonCode);

        builder.HasOne(x => x.BonusState)
            .WithMany()
            .HasForeignKey(x => x.BonusStateCode);
    }
}
