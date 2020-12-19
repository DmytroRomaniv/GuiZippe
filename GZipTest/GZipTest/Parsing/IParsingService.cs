using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Parsing
{
    public interface IParsingService
    {
        public string CompressionMode { get; set; }
        public string OriginalFileName { get; set; }
        public string ResultFileName { get; set; }
        bool TryParse(string[] args);
    }
}
