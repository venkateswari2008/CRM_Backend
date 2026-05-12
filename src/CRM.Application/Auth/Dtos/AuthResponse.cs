namespace CRM.Application.Auth.Dtos;

public sealed record AuthResponse(
    int UserId,
    string Username,
    string Email,
    string Role,
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);
