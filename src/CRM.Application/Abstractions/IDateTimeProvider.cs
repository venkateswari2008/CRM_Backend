namespace CRM.Application.Abstractions;

/// <summary>Abstraction over the system clock for deterministic testing.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}
