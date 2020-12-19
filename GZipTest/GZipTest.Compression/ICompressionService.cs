using GZipTest.Compression.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Compression
{
    public interface ICompressionService
    {
        CompressionResponse Compress(string originalFileName, string archiveFileName);

        CompressionResponse Decompress(string archiveFileName, string newFileName);
    }
}
