using LiteDB;

namespace SeoRender.Web.Data;

public class PreRenderDbContext : IDisposable
{
    private readonly LiteDatabase _metaDb;
    private readonly LiteDatabase _contentDb;

    public PreRenderDbContext(IConfiguration configuration)
    {
        var metaPath = configuration.GetConnectionString("PrerenderMetaConnection") ?? "Data/prerender_meta.db";
        var contentPath = configuration.GetConnectionString("PrerenderContentConnection") ?? "Data/prerender_content.db";

        _metaDb = new LiteDatabase(metaPath);
        _contentDb = new LiteDatabase(contentPath);

        Metas.EnsureIndex(p => p.Hash, unique: true);
        Metas.EnsureIndex(p => p.Domain);
        Contents.EnsureIndex(c => c.ContentHash, unique: true);
    }

    public ILiteCollection<RenderedPageMeta> Metas => _metaDb.GetCollection<RenderedPageMeta>("pages");
    public ILiteCollection<RenderedPageContent> Contents => _contentDb.GetCollection<RenderedPageContent>("contents");

    public void Dispose()
    {
        _metaDb.Dispose();
        _contentDb.Dispose();
    }
}

public class RenderedPageMeta
{
    [BsonId]
    public string Hash { get; set; } = default!;
    public string Domain { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string ContentHash { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}

public class RenderedPageContent
{
    [BsonId]
    public string ContentHash { get; set; } = default!;
    public byte[] Gzip { get; set; } = default!;
}
