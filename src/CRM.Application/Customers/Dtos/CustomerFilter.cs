using CRM.Application.Common;

namespace CRM.Application.Customers.Dtos;

public sealed class CustomerFilter : PageRequest
{
    public string? City { get; set; }

    public string? Country { get; set; }

    public string? Company { get; set; }
}
