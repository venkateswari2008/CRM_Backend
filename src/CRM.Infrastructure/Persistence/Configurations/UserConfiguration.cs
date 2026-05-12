using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");

        b.HasKey(x => x.Id);

        b.Property(x => x.Username).IsRequired().HasMaxLength(50);
        b.Property(x => x.Email).IsRequired().HasMaxLength(100);
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(255);
        b.Property(x => x.Role).IsRequired().HasMaxLength(20);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.Username).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
