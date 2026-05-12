using CRM.Application.Customers.Dtos;
using FluentValidation;

namespace CRM.Application.Customers.Validators;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(20)
            .Matches(@"^[+0-9 ()\-]*$").When(x => !string.IsNullOrWhiteSpace(x.Phone))
                .WithMessage("Phone must contain only digits, spaces, parentheses, '+' or '-'.");
        RuleFor(x => x.AddressLine).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(80);
        RuleFor(x => x.State).MaximumLength(80);
        RuleFor(x => x.ZipCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(80);
        RuleFor(x => x.Company).MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
