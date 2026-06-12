using CRM.Application.Common;

namespace CRM.UnitTests.Common;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_RoundsUp()
    {
        var p = new PagedResult<int>(new[] { 1, 2, 3 }, totalCount: 25, page: 1, pageSize: 10);
        p.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_IsZeroWhenPageSizeIsZero()
    {
        var p = new PagedResult<int>(Array.Empty<int>(), 0, 1, 0);
        p.TotalPages.Should().Be(0);
    }

    [Fact]
    public void HasNext_IsTrueWhenPageBelowTotal()
    {
        var p = new PagedResult<int>(new[] { 1 }, totalCount: 30, page: 2, pageSize: 10);
        p.HasNext.Should().BeTrue();
        p.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public void HasPrevious_IsFalseOnFirstPage()
    {
        var p = new PagedResult<int>(new[] { 1 }, totalCount: 5, page: 1, pageSize: 10);
        p.HasPrevious.Should().BeFalse();
        p.HasNext.Should().BeFalse();
    }
}
