using PuppeteerSharp;

namespace SeoRender.Web.Data;

public class PreRenderService
{
    private readonly PreRenderDbContext _db;
    private readonly ILogger<PreRenderService> _logger;

    public PreRenderService(PreRenderDbContext db, ILogger<PreRenderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RenderedPage> GetRenderedPageAsync(string url)
    {
        var hash = HashUtil.Sha256(url);
        var cached = _db.Pages.FindById(hash);
        if (cached != null)
        {
            _logger.LogInformation("Returning cached render for {Url}", url);
            return cached;
        }

        await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        await using var page = await browser.NewPageAsync();
        await page.GoToAsync(url, WaitUntilNavigation.Networkidle0);
        var content = await page.GetContentAsync();

        var doc = new RenderedPage
        {
            Hash = hash,
            Domain = new Uri(url).Host,
            Url = url,
            Html = content,
            Timestamp = DateTime.UtcNow
        };
        _db.Pages.Upsert(doc);
        return doc;
    }

    public async Task<string> GetRenderedHtmlAsync(string url)
    {
        var page = await GetRenderedPageAsync(url);
        return page.Html;
    }
}
