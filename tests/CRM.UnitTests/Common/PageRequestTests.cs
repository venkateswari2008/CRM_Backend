using CRM.Application.Common;

namespace CRM.UnitTests.Common;

public class PageRequestTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        var req = new PageRequest();
        req.Page.Should().Be(1);
        req.PageSize.Should().Be(20);
        req.Skip.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-5, 20)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(1000, 100)]
    public void PageSize_ClampsBetween1And100_AndDefaultsTo20OnNonPositive(int input, int expected)
    {
        var req = new PageRequest { PageSize = input };
        req.PageSize.Should().Be(expected);
    }

    [Fact]
    public void Skip_NeverNegative()
    {
        var req = new PageRequest { Page = -1, PageSize = 10 };
        req.Skip.Should().Be(0);
    }

    [Fact]
    public void Skip_IsZeroBasedOffset()
    {
        var req = new PageRequest { Page = 3, PageSize = 25 };
        req.Skip.Should().Be(50);
    }
}
