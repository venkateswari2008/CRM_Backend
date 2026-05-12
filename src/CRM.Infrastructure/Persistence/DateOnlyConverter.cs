using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CRM.Infrastructure.Persistence;

/// <summary>
/// Converter that lets EF Core map <see cref="DateOnly"/> to <see cref="DateTime"/>
/// for providers (e.g. SQLite) that lack a native DateOnly type.
/// </summary>
public sealed class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
{
    public DateOnlyConverter()
        : base(d => d.ToDateTime(TimeOnly.MinValue),
               dt => DateOnly.FromDateTime(dt))
    {
    }
}

public sealed class NullableDateOnlyConverter : ValueConverter<DateOnly?, DateTime?>
{
    public NullableDateOnlyConverter()
        : base(d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
               dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : (DateOnly?)null)
    {
    }
}
