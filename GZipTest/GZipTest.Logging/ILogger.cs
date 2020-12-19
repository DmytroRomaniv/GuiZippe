using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Logging
{
    public interface ILogger
    {
        void Log(string message);
        void Warning(string message);
        void Error(string message);
    }
}
