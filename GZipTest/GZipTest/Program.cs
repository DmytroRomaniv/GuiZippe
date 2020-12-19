using GZipTest.Compression;
using GZipTest.Logging;
using GZipTest.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Timers;

namespace GZipTest
{
    public static class Program
    {
        private const string CompressionMode = "compress";
        private const string DecompressionMode = "decompress";

        private static readonly IParsingService parsingService;
        private static readonly ICompressionService compressionService;
        private static readonly ILogger logger;

        static Program()
        {
            var cancellationToken = new CancellationTokenSource();

            logger = new ConsoleLogger();
            parsingService = new ParsingService(logger);
            compressionService = new CompressionService(cancellationToken, logger);
        }

        public static int Main(string[] args)
        {
            //args = new string[] { "decompress", "huge_dummy_file.gz", "new_huge_dummy_file" };
            if(parsingService.TryParse(args))
            {
                int result;
                switch (parsingService.CompressionMode)
                {
                    case CompressionMode:
                        result = (int)compressionService.Compress(parsingService.OriginalFileName, parsingService.ResultFileName); 
                        break;
                    case DecompressionMode:
                        result = (int)compressionService.Decompress(parsingService.OriginalFileName, parsingService.ResultFileName);
                        break;
                    default:
                        result = 1;
                        logger.Warning("Compression mode can be either 'compress' or 'decompress'");
                        break;
                }

                return result;
            }

            return 1;
            //using(var md5 = MD5.Create())
            //{
            //    byte[] originalFileHash;
            //    byte[] newFileHash;
            //    using(var stream = File.OpenRead(fileName))
            //    {
            //        originalFileHash = md5.ComputeHash(stream);
            //    }

            //    using(var stream = File.OpenRead(newFile))
            //    {
            //        newFileHash = md5.ComputeHash(stream);
            //    }

            //    Console.WriteLine(originalFileHash.SequenceEqual(newFileHash));
            //}
        }
    }
}
