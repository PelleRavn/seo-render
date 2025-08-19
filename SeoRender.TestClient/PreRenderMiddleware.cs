using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using System.Linq;

namespace SeoRender.TestClient;

public class PreRenderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PreRenderOptions _options;
    private static readonly string[] _botSignatures = new[]
    {
        "googlebot",
        "bingbot",
        "slurp",
        "duckduckbot",
        "baiduspider",
        "yandex"
    };

    public PreRenderMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, IOptions<PreRenderOptions> options)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ua = context.Request.Headers.UserAgent.ToString().ToLowerInvariant();
        var isBot = _options.ForcePrerender || _botSignatures.Any(ua.Contains);

        if (!isBot || context.Request.Method != HttpMethods.Get)
        {
            await _next(context);
            return;
        }

        var pageUrl = UriHelper.GetDisplayUrl(context.Request);
        var target = $"{_options.ServerBaseUrl.TrimEnd('/')}/api/prerender?url={Uri.EscapeDataString(pageUrl)}";

        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(target);

        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await response.Content.CopyToAsync(context.Response.Body);
    }
}
