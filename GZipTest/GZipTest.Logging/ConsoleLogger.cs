using System;

namespace GZipTest.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void Error(string message)
        {
            if (!string.IsNullOrEmpty(message))
                Console.WriteLine($"ERROR: {message}");
        }

        public void Log(string message)
        {
            if(!string.IsNullOrEmpty(message))
                Console.WriteLine(message);
        }

        public void Warning(string message)
        {
            if (!string.IsNullOrEmpty(message))
                Console.WriteLine($"WARNING: {message}");
        }
    }
}
