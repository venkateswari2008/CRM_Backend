using CRM.Api.Extensions;
using CRM.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CRM.IntegrationTests.Unit;

public class ResultExtensionsTests
{
    private static FakeController NewController()
    {
        var ctrl = new FakeController();
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        ctrl.ControllerContext.HttpContext.Request.Path = "/api/test";
        return ctrl;
    }

    [Fact]
    public void ToActionResult_Success_ReturnsOk()
    {
        var r = Result<string>.Success("ok").ToActionResult(NewController());
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(ResultErrorCodes.NotFound, 404)]
    [InlineData(ResultErrorCodes.Duplicate, 409)]
    [InlineData(ResultErrorCodes.Conflict, 409)]
    [InlineData(ResultErrorCodes.Validation, 400)]
    [InlineData(ResultErrorCodes.Unauthorized, 401)]
    [InlineData(ResultErrorCodes.Forbidden, 403)]
    [InlineData("unknown_code", 500)]
    public void ToActionResult_Failure_MapsToCorrectStatus(string code, int expected)
    {
        var r = Result<string>.Failure("err", code).ToActionResult(NewController());

        var statusCode = r.Result switch
        {
            ObjectResult o => o.StatusCode,
            _ => null,
        };
        statusCode.Should().Be(expected);
    }

    [Fact]
    public void ToNoContentResult_Success_ReturnsNoContent()
    {
        var r = Result<bool>.Success(true).ToNoContentResult(NewController());
        r.Should().BeOfType<NoContentResult>();
    }

    [Theory]
    [InlineData(ResultErrorCodes.NotFound, 404)]
    [InlineData(ResultErrorCodes.Conflict, 409)]
    [InlineData(ResultErrorCodes.Validation, 400)]
    public void ToNoContentResult_Failure_MapsToCorrectStatus(string code, int expected)
    {
        var r = Result<bool>.Failure("err", code).ToNoContentResult(NewController());
        var statusCode = r switch
        {
            ObjectResult o => o.StatusCode,
            _ => null,
        };
        statusCode.Should().Be(expected);
    }

    private sealed class FakeController : ControllerBase
    {
    }
}
