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

        // Filter the unique indexes so soft-deleted rows do not block a live row from
        // reusing the same username/email. Mirrors the HasQueryFilter on IsDeleted below.
        b.HasIndex(x => x.Username).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => x.Email).IsUnique().HasFilter("[IsDeleted] = 0");

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
