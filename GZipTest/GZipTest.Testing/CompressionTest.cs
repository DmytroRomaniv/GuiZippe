using GZipTest.Compression;
using GZipTest.Logging;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace GZipTest.Testing
{
    [TestFixture]
    public class Tests
    {
        private ICompressionService compressionService;

        [SetUp]
        public void Setup()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var mockLogger = new Mock<ILogger>();

            mockLogger.Setup(foo => foo.Warning(It.IsAny<string>()));
            mockLogger.Setup(foo => foo.Error(It.IsAny<string>()));
            compressionService = new CompressionService(cancellationTokenSource, mockLogger.Object);
        }

        [TestCase("entities/huge_dummy_file", "entities/huge_dummy_file.gz", "entities/decompressed_huge_dummy_file")]
        [TestCase("entities/small_dummy_file", "entities/small_dummy_file.gz", "entities/decompressed_small_dummy_file")]
        public void BigFileCompressionTest(string originalFileName, string archiveFileName, string decompressedFileName)
        {
            _ = compressionService.Compress(originalFileName, archiveFileName);
            _ = compressionService.Decompress(archiveFileName, decompressedFileName);

            Assert.True(CheckHashSum(originalFileName, decompressedFileName));
        }

        private bool CheckHashSum(string originalFileName, string decompressedFileName)
        {
            using (var md5 = MD5.Create())
            {
                byte[] originalFileHash;
                byte[] newFileHash;
                using (var stream = File.OpenRead(originalFileName))
                {
                    originalFileHash = md5.ComputeHash(stream);
                }

                using (var stream = File.OpenRead(decompressedFileName))
                {
                    newFileHash = md5.ComputeHash(stream);
                }

                return originalFileHash.SequenceEqual(newFileHash);
            }
        }
    }
}