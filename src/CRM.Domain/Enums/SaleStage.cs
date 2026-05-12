namespace CRM.Domain.Enums;

public static class SaleStages
{
    public const string Qualification = "Qualification";
    public const string Proposal = "Proposal";
    public const string Negotiation = "Negotiation";
    public const string ClosedWon = "ClosedWon";
    public const string ClosedLost = "ClosedLost";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Qualification, Proposal, Negotiation, ClosedWon, ClosedLost,
    };

    public static IReadOnlyList<string> ClosedStages { get; } = new[]
    {
        ClosedWon, ClosedLost,
    };

    public static bool IsValid(string? stage) => stage is not null && All.Contains(stage);

    public static bool IsClosed(string? stage) => stage is not null && ClosedStages.Contains(stage);
}
