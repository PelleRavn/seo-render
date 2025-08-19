# Seo Render

This solution contains two ASP.NET Core projects:

- **SeoRender.Web** – prerendering server that renders pages and caches the result.
- **SeoRender.TestClient** – sample site that uses middleware to request prerendered pages for bots.

The server marks its responses with the `X-From-SeoRender` header so that you can verify when content was served by the prerenderer.

## Testing in a browser

1. Build the solution:
   ```bash
   dotnet build SeoRender.sln -c Release
   ```
2. Run the prerender server:
   ```bash
   dotnet run --project SeoRender.Web
   ```
3. In a separate terminal, run the test client:
   ```bash
   dotnet run --project SeoRender.TestClient
   ```
4. Open the client in a browser (default `http://localhost:5294`). In development the client is configured to prerender all requests. Inspect the network tab of your browser's developer tools and confirm the response contains the `X-From-SeoRender: true` header.
5. To test user agent detection, set `PreRender:ForcePrerender` to `false` in `SeoRender.TestClient/appsettings.Development.json`. Reload the client and change your browser's user agent to a bot string (e.g. `Googlebot`). Requests with a bot user agent will receive prerendered content and include the `X-From-SeoRender` header.

