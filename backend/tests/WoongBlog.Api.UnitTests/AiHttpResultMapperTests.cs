using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WoongBlog.Api.Modules.AI;
using WoongBlog.Application.Modules.AI;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Unit)]
public sealed class AiHttpResultMapperTests
{
    [Fact]
    public async Task ToHttpResult_WithOkStatus_ReturnsHttp200()
    {
        var result = await ExecuteAsync(AiActionResult<string>.Ok("created"));

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public async Task ToHttpResult_WithBadRequestStatus_ReturnsHttp400WithErrorBody()
    {
        var result = await ExecuteAsync(AiActionResult<string>.BadRequest("invalid request"));

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("invalid request", result.Body);
    }

    [Fact]
    public async Task ToHttpResult_WithNotFoundStatus_ReturnsHttp404()
    {
        var result = await ExecuteAsync(AiActionResult<string>.NotFound());

        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public async Task ToHttpResult_WithConflictStatus_ReturnsHttp409WithErrorBody()
    {
        var result = await ExecuteAsync(AiActionResult<string>.Conflict("already running"));

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Contains("already running", result.Body);
    }

    [Fact]
    public async Task ToHttpResult_WithUnknownStatus_ReturnsHttp500()
    {
        var result = await ExecuteAsync(new AiActionResult<string>((AiActionStatus)999, null, "unexpected"));

        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    private static async Task<ExecutedHttpResult> ExecuteAsync<T>(AiActionResult<T> actionResult)
    {
        var httpResult = actionResult.ToHttpResult();
        var context = new DefaultHttpContext();
        using var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        await using var body = new MemoryStream();
        context.RequestServices = services;
        context.Response.Body = body;

        await httpResult.ExecuteAsync(context);

        body.Position = 0;
        using var reader = new StreamReader(body);
        return new ExecutedHttpResult(context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private sealed record ExecutedHttpResult(int StatusCode, string Body);
}
