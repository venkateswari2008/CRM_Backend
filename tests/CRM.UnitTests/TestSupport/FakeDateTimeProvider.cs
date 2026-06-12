using CRM.Application.Abstractions;

namespace CRM.UnitTests.TestSupport;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider() : this(new DateTimeOffset(2026, 6, 12, 12, 0, 0, TimeSpan.Zero)) { }

    public FakeDateTimeProvider(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
