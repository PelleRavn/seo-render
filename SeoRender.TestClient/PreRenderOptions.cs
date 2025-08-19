namespace SeoRender.TestClient;

public class PreRenderOptions
{
    public string ServerBaseUrl { get; set; } = "https://localhost:7216";
    public bool ForcePrerender { get; set; }
        = false;
}
