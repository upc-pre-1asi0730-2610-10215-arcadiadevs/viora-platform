using System.Net.Mime;
using System.Text.Json;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Pipeline.Middleware.Components;
using ArcadiaDevs.Viora.Platform.Shared.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Unit tests for <see cref="GlobalExceptionHandlerMiddleware"/>.
///     Verifies the three exception-to-HTTP-status mappings:
///     DbUpdateException → 409, generic Exception → 500,
///     OperationCanceledException → 409.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class GlobalExceptionHandlerMiddlewareTests
{
    private static IStringLocalizer<ErrorMessages> StubErrorLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<ErrorMessages>>();
        localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), value: call.ArgAt<string>(0)));
        return localizer;
    }

    private static IStringLocalizer<CommonMessages> StubCommonLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<CommonMessages>>();
        localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), value: call.ArgAt<string>(0)));
        return localizer;
    }

    private static ILogger<GlobalExceptionHandlerMiddleware> StubLogger()
    {
        return Substitute.For<ILogger<GlobalExceptionHandlerMiddleware>>();
    }

    /// <summary>
    ///     Creates a <see cref="DefaultHttpContext"/> with a seekable
    ///     <see cref="MemoryStream"/> as the response body so tests can
    ///     read back written content.
    /// </summary>
    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Response = { StatusCode = 0, Body = new MemoryStream() }
        };
    }

    [Fact]
    public async Task Handle_DbUpdateException_Returns409()
    {
        // Arrange — RequestDelegate that throws DbUpdateException (WU9 mapping)
        RequestDelegate next = _ => throw new DbUpdateException("unique constraint violation");
        var middleware = new GlobalExceptionHandlerMiddleware(
            next, StubLogger(), StubErrorLocalizer(), StubCommonLocalizer());
        var httpContext = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert — 409 Conflict with DbConflict localized message
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, httpContext.Response.ContentType);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Contains("DbConflict", body);
    }

    [Fact]
    public async Task Handle_GeneralException_Returns500()
    {
        // Arrange — RequestDelegate that throws a generic exception
        RequestDelegate next = _ => throw new InvalidOperationException("something broke");
        var middleware = new GlobalExceptionHandlerMiddleware(
            next, StubLogger(), StubErrorLocalizer(), StubCommonLocalizer());
        var httpContext = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert — 500 Internal Server Error
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, httpContext.Response.ContentType);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Contains("InternalServerError", body);
    }

    [Fact]
    public async Task Handle_CancellationOperation_Returns409()
    {
        // Arrange — OperationCanceledException is caught and mapped to 409
        var cts = new CancellationTokenSource();
        cts.Cancel();
        RequestDelegate next = _ => throw new OperationCanceledException(cts.Token);
        var middleware = new GlobalExceptionHandlerMiddleware(
            next, StubLogger(), StubErrorLocalizer(), StubCommonLocalizer());
        var httpContext = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert — 409 Conflict with OperationCancelled localized message
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, httpContext.Response.ContentType);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Contains("OperationCancelled", body);
    }
}
