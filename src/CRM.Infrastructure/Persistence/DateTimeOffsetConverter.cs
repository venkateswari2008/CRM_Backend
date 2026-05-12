using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CRM.Infrastructure.Persistence;

/// <summary>
/// Stores DateTimeOffset as UTC ticks (long) so SQLite can compare/order values
/// natively. Values are always normalised to UTC on the way in.
/// </summary>
public sealed class DateTimeOffsetToTicksConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToTicksConverter()
        : base(d => d.UtcTicks,
               t => new DateTimeOffset(t, TimeSpan.Zero))
    {
    }
}

public sealed class NullableDateTimeOffsetToTicksConverter : ValueConverter<DateTimeOffset?, long?>
{
    public NullableDateTimeOffsetToTicksConverter()
        : base(d => d.HasValue ? d.Value.UtcTicks : (long?)null,
               t => t.HasValue ? new DateTimeOffset(t.Value, TimeSpan.Zero) : (DateTimeOffset?)null)
    {
    }
}
