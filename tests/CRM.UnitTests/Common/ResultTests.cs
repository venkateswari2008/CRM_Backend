using CRM.Application.Common;

namespace CRM.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasValueAndNoError()
    {
        var r = Result<string>.Success("ok");

        r.IsSuccess.Should().BeTrue();
        r.IsFailure.Should().BeFalse();
        r.Value.Should().Be("ok");
        r.Error.Should().BeNull();
        r.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void Failure_HasErrorAndOptionalCode()
    {
        var r = Result<string>.Failure("boom", ResultErrorCodes.Validation);

        r.IsSuccess.Should().BeFalse();
        r.IsFailure.Should().BeTrue();
        r.Value.Should().BeNull();
        r.Error.Should().Be("boom");
        r.ErrorCode.Should().Be("validation");
    }

    [Fact]
    public void Failure_WithoutCode_LeavesCodeNull()
    {
        var r = Result<int>.Failure("nope");
        r.ErrorCode.Should().BeNull();
    }

    [Theory]
    [InlineData(ResultErrorCodes.NotFound, "not_found")]
    [InlineData(ResultErrorCodes.Duplicate, "duplicate")]
    [InlineData(ResultErrorCodes.Validation, "validation")]
    [InlineData(ResultErrorCodes.Unauthorized, "unauthorized")]
    [InlineData(ResultErrorCodes.Forbidden, "forbidden")]
    [InlineData(ResultErrorCodes.Conflict, "conflict")]
    public void ErrorCode_ConstantsMatchExpectedStrings(string code, string expected)
    {
        code.Should().Be(expected);
    }
}
