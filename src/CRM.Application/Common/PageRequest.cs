namespace CRM.Application.Common;

public class PageRequest
{
    private const int MaxPageSize = 100;

    private int _pageSize = 20;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value <= 0 ? 20 : Math.Min(value, MaxPageSize);
    }

    public string? Sort { get; set; }

    public string? Search { get; set; }

    public int Skip => Math.Max(0, (Page - 1) * PageSize);
}
