using PuppeteerSharp;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Threading;

namespace SeoRender.Web.Data;

public class PreRenderService
{
    private readonly PreRenderDbContext _db;
    private readonly ILogger<PreRenderService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

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
            var cachedContent = _db.Contents.FindById(cachedMeta.ContentHash)!;
            cachedMeta.Timestamp = DateTime.UtcNow;
            _db.Metas.Update(cachedMeta);
            _logger.LogInformation("Returning cached render for {Url}", url);
            return new RenderedPageResult(cachedMeta, cachedContent.Gzip);
        }

        var sem = _locks.GetOrAdd(hash, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            // Another request might have finished rendering while waiting
            if (_db.Metas.FindById(hash) is { } meta)
            {
                var existingContent = _db.Contents.FindById(meta.ContentHash)!;
                meta.Timestamp = DateTime.UtcNow;
                _db.Metas.Update(meta);
                return new RenderedPageResult(meta, existingContent.Gzip);
            }

            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            await using var page = await browser.NewPageAsync();
            await page.GoToAsync(url, WaitUntilNavigation.Networkidle0);
            var html = await page.GetContentAsync();

            var contentHash = HashUtil.Sha256(html);
            var gzip = CompressionUtil.CompressString(html);

            if (_db.Contents.FindById(contentHash) == null)
            {
                _db.Contents.Insert(new RenderedPageContent
                {
                    ContentHash = contentHash,
                    Gzip = gzip
                });
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
            return new RenderedPageResult(doc, gzip);
        }
        finally
        {
            sem.Release();
            _locks.TryRemove(hash, out _);
        }
    }

    public async Task<string> GetRenderedHtmlAsync(string url)
    {
        var page = await GetRenderedPageAsync(url);
        return CompressionUtil.DecompressString(page.GzipContent);
    }
}

public record RenderedPageResult(RenderedPageMeta Meta, byte[] GzipContent);
