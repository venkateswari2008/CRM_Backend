namespace CRM.Application.Dashboard.Dtos;

public sealed record DashboardDto(
    decimal TotalSales,
    int NewLeads,
    int OpenOpportunities,
    int WonDeals,
    int LostDeals,
    IReadOnlyList<MonthlySalesDto> MonthlySales,
    IReadOnlyList<StageBreakdownDto> StageBreakdown,
    IReadOnlyList<TopCustomerDto> TopCustomers);

public sealed record MonthlySalesDto(int Year, int Month, decimal Total, int Count);

public sealed record StageBreakdownDto(string Stage, decimal Total, int Count);

public sealed record TopCustomerDto(int CustomerId, string CustomerName, string? Company, decimal Total, int DealCount);
