using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.Exceptions;
using Shared.Filters;
using Shared.Middleware;

namespace AuthService.Tests;

public class ErrorResponseTests
{
    private static ExceptionContext CreateContext(Exception error)
    {
        var http = new DefaultHttpContext { TraceIdentifier = "request-42" };
        return new ExceptionContext(new ActionContext(http, new RouteData(), new ActionDescriptor()),
            Array.Empty<IFilterMetadata>())
        { Exception = error };
    }

    [Theory]
    [InlineData(false, 500, "INTERNAL_SERVER_ERROR", LogLevel.Error)]
    [InlineData(true, 409, "BUSINESS_RULE_VIOLATION", LogLevel.Warning)]
    public void Filter_ReturnsSafeErrorsAndLogsOriginalException(bool business, int status, string code, LogLevel level)
    {
        Exception error = business ? new BusinessRuleException("Order is closed") : new Exception("Password=secret; internal SQL");
        var context = CreateContext(error);
        var logger = new Mock<ILogger<GlobalExceptionFilter>>();

        new GlobalExceptionFilter(logger.Object).OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(status, result.StatusCode);
        Assert.True(context.ExceptionHandled);
        using var body = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal(code, body.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("request-42", body.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal(business ? "Order is closed" : "An unexpected server error occurred.",
            body.RootElement.GetProperty("message").GetString());
        Assert.False(body.RootElement.TryGetProperty("detail", out _));
        logger.Verify(x => x.Log(level, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), error,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Filter_DoesNotReplaceHandledOrStartedResponses(bool handled)
    {
        var context = CreateContext(new Exception("original"));
        context.ExceptionHandled = handled;
        if (!handled)
        {
            var response = new Mock<IHttpResponseFeature>();
            response.SetupGet(x => x.HasStarted).Returns(true);
            context.HttpContext.Features.Set(response.Object);
        }
        new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance).OnException(context);
        Assert.Null(context.Result);
        Assert.Equal(handled, context.ExceptionHandled);
    }

    [Theory]
    [InlineData(false, 500, "INTERNAL_SERVER_ERROR")]
    [InlineData(true, 409, "BUSINESS_RULE_VIOLATION")]
    public async Task Middleware_ReturnsSafeJsonAndCorrelation(bool business, int status, string code)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "request-42" };
        await using var stream = new MemoryStream();
        context.Response.Body = stream;
        Exception error = business ? new BusinessRuleException("Order is closed") : new Exception("Password=secret");
        var middleware = new ExceptionHandlingMiddleware(_ => throw error,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(status, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        stream.Position = 0;
        using var body = await JsonDocument.ParseAsync(stream);
        Assert.Equal(code, body.RootElement.GetProperty("ErrorCode").GetString());
        Assert.Equal("request-42", body.RootElement.GetProperty("CorrelationId").GetString());
        Assert.DoesNotContain("Password=secret", body.RootElement.GetRawText());
        if (business)
            Assert.Equal("Order is closed", body.RootElement.GetProperty("Message").GetString());
    }

    [Fact]
    public async Task Middleware_PreservesSuccessfulResponse()
    {
        var context = new DefaultHttpContext();
        await using var stream = new MemoryStream();
        context.Response.Body = stream;
        var middleware = new ExceptionHandlingMiddleware(async http =>
        {
            http.Response.StatusCode = 201;
            await http.Response.WriteAsync("created");
        }, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(201, context.Response.StatusCode);
        Assert.Equal("created", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }
}
