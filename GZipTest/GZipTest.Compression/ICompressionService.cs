using GZipTest.Compression.Results;

namespace GZipTest.Compression
{
    public interface ICompressionService
    {
        CompressionResult Compress(string originalFileName, string archiveFileName);

        CompressionResult Decompress(string archiveFileName, string newFileName);
    }
}
