namespace CRM.Application.Auth.Dtos;

public sealed record SignupRequest(
    string Username,
    string Email,
    string Password,
    string? Role = null);
