using CRM.Application.Sales.Dtos;
using CRM.Domain.Enums;
using FluentValidation;

namespace CRM.Application.Sales.Validators;

public sealed class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);
        RuleFor(x => x.PipelineName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Stage).NotEmpty().Must(SaleStages.IsValid)
            .WithMessage($"Stage must be one of: {string.Join(", ", SaleStages.All)}.");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).LessThan(1_000_000_000m);
        RuleFor(x => x.SaleDate).NotEmpty();
        RuleFor(x => x.ExpectedCloseDate)
            .GreaterThanOrEqualTo(x => x.SaleDate)
            .When(x => x.ExpectedCloseDate.HasValue)
            .WithMessage("Expected close date must be on or after the sale date.");
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
