using PuppeteerSharp;
using System.IO;
using System.IO.Compression;

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

    public async Task<RenderedPageResult> GetRenderedPageAsync(string url)
    {
        var hash = HashUtil.Sha256(url);

        if (_db.Metas.FindById(hash) is { } cachedMeta)
        {
            cachedMeta.Timestamp = DateTime.UtcNow;
            _db.Metas.Update(cachedMeta);
            _logger.LogInformation("Returning cached render for {Url}", url);
            var stream = _db.Storage.OpenRead(cachedMeta.ContentHash);
            return new RenderedPageResult(cachedMeta, stream);
        }

        await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        await using var page = await browser.NewPageAsync();
        await page.GoToAsync(url, WaitUntilNavigation.Networkidle0);
        var html = await page.GetContentAsync();

        var contentHash = HashUtil.Sha256(html);
        var gzip = CompressionUtil.CompressString(html);

        if (!_db.Storage.Exists(contentHash))
        {
            using var ms = new MemoryStream(gzip, writable: false);
            _db.Storage.Upload(contentHash, contentHash + ".gz", ms);
        }

        var doc = new RenderedPageMeta
        {
            Hash = hash,
            Domain = new Uri(url).Host,
            Url = url,
            ContentHash = contentHash,
            Timestamp = DateTime.UtcNow
        };

        _db.Metas.Upsert(doc);
        var resultStream = _db.Storage.OpenRead(contentHash);
        return new RenderedPageResult(doc, resultStream);
    }

    public async Task<string> GetRenderedHtmlAsync(string url)
    {
        using var result = await GetRenderedPageAsync(url);
        using var decompressed = new GZipStream(result.GzipStream, CompressionMode.Decompress);
        using var reader = new StreamReader(decompressed);
        return await reader.ReadToEndAsync();
    }
}

public record RenderedPageResult(RenderedPageMeta Meta, Stream GzipStream) : IDisposable
{
    public void Dispose() => GzipStream.Dispose();
}
