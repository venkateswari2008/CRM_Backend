using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> b)
    {
        b.ToTable("Sales");

        b.HasKey(x => x.Id);

        b.Property(x => x.PipelineName).IsRequired().HasMaxLength(100);
        b.Property(x => x.Stage).IsRequired().HasMaxLength(40);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.User)
            .WithMany(u => u.OwnedSales)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Stage);
        b.HasIndex(x => x.SaleDate);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
