using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");

        b.HasKey(x => x.Id);

        b.Property(x => x.FirstName).IsRequired().HasMaxLength(50);
        b.Property(x => x.LastName).IsRequired().HasMaxLength(50);
        b.Property(x => x.Email).IsRequired().HasMaxLength(100);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.AddressLine).HasMaxLength(200);
        b.Property(x => x.City).HasMaxLength(80);
        b.Property(x => x.State).HasMaxLength(80);
        b.Property(x => x.ZipCode).HasMaxLength(20);
        b.Property(x => x.Country).HasMaxLength(80);
        b.Property(x => x.Company).HasMaxLength(120);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.Ignore(x => x.FullName);

        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => new { x.LastName, x.FirstName });
        b.HasIndex(x => x.Company);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
