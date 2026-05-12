using CRM.Application.Auth.Dtos;
using CRM.Domain.Enums;
using FluentValidation;

namespace CRM.Application.Auth.Validators;

public sealed class SignupRequestValidator : AbstractValidator<SignupRequest>
{
    public SignupRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Matches(@"^[a-zA-Z0-9_.-]+$")
                .WithMessage("Username may only contain letters, digits, underscores, dots and hyphens.")
            .MinimumLength(3)
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

        RuleFor(x => x.Role)
            .Must(r => r is null || UserRoles.IsValid(r))
                .WithMessage($"Role must be one of: {string.Join(", ", UserRoles.All)}.");
    }
}
