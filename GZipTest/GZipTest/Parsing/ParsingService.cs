using GZipTest.Compression;
using GZipTest.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Parsing
{
    public class ParsingService : IParsingService
    {
        private const int NumberOfArguments = 3;

        private readonly ILogger logger;

        public string CompressionMode { get; set; }
        public string OriginalFileName { get; set; }
        public string ResultFileName { get; set; }

        public ParsingService(ILogger logger)
        {
            this.logger = logger;
        }
        public bool TryParse(string[] args)
        {
            if (args.Length < NumberOfArguments)
            {
                logger.Warning("The valid number of arguments is 3. Example: compress/decompress [source file name] [result file name]");
                return false;
            }

            CompressionMode = args[0];
            OriginalFileName = args[1];
            ResultFileName = args[2];

            return true;
        }
    }
}
