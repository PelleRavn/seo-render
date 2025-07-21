using System.IO.Compression;
using System.Text;

namespace SeoRender.Web.Data;

public static class CompressionUtil
{
    public static byte[] CompressString(string input)
    {
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
        using (var writer = new StreamWriter(gzip, Encoding.UTF8))
        {
            writer.Write(input);
        }
        return ms.ToArray();
    }

    public static string DecompressString(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var gzip = new GZipStream(ms, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
