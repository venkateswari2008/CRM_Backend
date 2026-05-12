namespace CRM.Application.Auth.Dtos;

public sealed record LoginRequest(string UsernameOrEmail, string Password);
