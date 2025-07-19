using LiteDB;

namespace SeoRender.Web.Data;

public class PreRenderDbContext : IDisposable
{
    private readonly LiteDatabase _database;

    public PreRenderDbContext(IConfiguration configuration)
    {
        var path = configuration.GetConnectionString("PrerenderConnection") ?? "Data/prerender.db";
        _database = new LiteDatabase(path);
        Pages.EnsureIndex(p => p.Hash, unique: true);
        Pages.EnsureIndex(p => p.Domain);
    }

    public ILiteCollection<RenderedPage> Pages => _database.GetCollection<RenderedPage>("pages");

    public void Dispose() => _database.Dispose();
}

public class RenderedPage
{
    [BsonId]
    public string Hash { get; set; } = default!;
    public string Domain { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string Html { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}
