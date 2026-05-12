namespace CRM.Domain.Enums;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static IReadOnlyList<string> All { get; } = new[] { Admin, User };

    public static bool IsValid(string? role) => role is not null && All.Contains(role);
}
