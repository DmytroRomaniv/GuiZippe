using GZipTest.Compression.Results;
using GZipTest.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipTest.Compression
{
    public class CompressionService:ICompressionService
    {
        private const int MegaByteArrayLength = 1048576;
        private const byte NullValueByte = 0;
        private const int Timeout = 1;
        private const int EmptyBlockValue = -1;
        private const string DefaultArchiveExtension = ".gz";

        private readonly int numberOfThreads = Environment.ProcessorCount;
        private readonly string[] archiveFileExtentions = new string[] { ".gz", ".zip", ".rar", ".7z", ".tar" };

        private readonly object inputLock = new object();
        private readonly object outputLock = new object();
        private readonly object readBlockIndexLock = new object();
        private readonly object writeBlockIndexLock = new object();

        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly ILogger logger; 

        private int readBlockIndex;
        private int writeBlockIndex;

        private int ReadBlockIndex { 
            get 
            {
                lock (readBlockIndexLock)
                {
                    return readBlockIndex;
                }
            }
            set
            {
                lock(readBlockIndexLock)
                {
                    readBlockIndex = value;
                }
            }
        }

        private int WriteBlockIndex
        {
            get
            {
                lock(writeBlockIndexLock)
                {
                    return writeBlockIndex;
                }
            }
            set
            {
                lock (writeBlockIndexLock)
                {
                    writeBlockIndex = value;
                }
            }
        }

        private bool IsCanceled
        {
            get
            {
                return cancellationTokenSource.IsCancellationRequested;
            }
        }

        public CompressionService(CancellationTokenSource cancellationTokenSource, ILogger logger)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            this.logger = logger ?? new ConsoleLogger();
        }

        public CompressionResult Compress(string originalFileName, string archiveFileName)
        {
            if(string.IsNullOrEmpty(originalFileName) || string.IsNullOrEmpty(archiveFileName))
            {
                logger.Warning("File name cannot be empty");
                return CompressionResult.NotFinished;
            }

            archiveFileName = AddArchiveExtension(archiveFileName);

            var compressionMode = CompressionMode.Compress;
            var compressionResponse = RunCompression(originalFileName, archiveFileName, compressionMode);

            return compressionResponse;
        }

        public CompressionResult Decompress(string archiveFileName, string newFileName)
        {
            if (string.IsNullOrEmpty(archiveFileName) || string.IsNullOrEmpty(newFileName))
            {
                logger.Warning("File name cannot be empty");
                return CompressionResult.NotFinished;
            }

            if (!IsFileArchive(archiveFileName))
            {
                logger.Warning($"File: {archiveFileName}, is not a compressed file.");
                return CompressionResult.NotFinished;
            }    

            var compressionMode = CompressionMode.Decompress;
            var compressionResponse = RunCompression(archiveFileName, newFileName, compressionMode);

            return compressionResponse;
        }

        private void SetDefaultValuesForIndexes()
        {
            readBlockIndex = EmptyBlockValue;
            writeBlockIndex = default;
        }


        private void DeleteFileIfExists(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        private bool FileExists(string fileName)
        {
            return File.Exists(fileName);
        }

        private string AddArchiveExtension(string fileName)
        {
            if (IsFileArchive(fileName))
                return fileName;
            else
                return fileName + DefaultArchiveExtension;
        }

        private bool IsFileArchive(string fileName)
        {
            return archiveFileExtentions.Any(e => fileName.EndsWith(e));
        }

        private CompressionResult RunCompression(string originalFileName, string newFileName, CompressionMode compressionMode)
        {
            if (!FileExists(originalFileName))
            {
                logger.Warning($"File: {originalFileName}, does not exist.");
                return CompressionResult.NotFinished;
            }

            SetDefaultValuesForIndexes();
            DeleteFileIfExists(newFileName);

            try
            {
                using (var inputStream = new FileStream(originalFileName, FileMode.Open))
                {
                    using (var outputStream = new FileStream(newFileName, FileMode.Append))
                    {
                        if (compressionMode == CompressionMode.Compress)
                        {
                            using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                            {
                                RunParallel(CopyFromTo, inputStream, gZipStream);
                            }
                        }
                        else
                        {
                            using (var gZipStream = new GZipStream(inputStream, compressionMode))
                            {
                                RunParallel(CopyFromTo, gZipStream, outputStream);
                            }
                        }

                    }
                }
            }
            catch (IOException exception)
            {
                cancellationTokenSource.Cancel();
                logger.Error($"An error occured during {compressionMode}: {originalFileName}. {exception.Message}");
            }
            catch(Exception exception)
            {
                cancellationTokenSource.Cancel();
                logger.Error($"A generic exception occured during {compressionMode}: {originalFileName}. {exception.Message}");
            }

            return IsCanceled ? CompressionResult.NotFinished : CompressionResult.Finished;
        }

        private void RunParallel(Action<Stream, Stream> copyAction, Stream inputStream, Stream outputStream)
        {
            var threads = new Thread[numberOfThreads];

            for (var i = 0; i < numberOfThreads; i++)
            {
                threads[i] = new Thread(() => copyAction(inputStream, outputStream));
                threads[i].Start();
            }

            for (var i = 0; i < numberOfThreads; i++)
            {
                threads[i].Join();
            }
        }

        private void CopyFromTo(Stream inputStream, Stream outputStream)
        {
            int blockIndex = default;

            while (!IsCanceled && blockIndex != EmptyBlockValue)
            {
                blockIndex = ReadFromFile(inputStream, out var buffer);
                if (buffer.Any())
                {
                    while (!IsCanceled && blockIndex != WriteBlockIndex)
                    {
                        Thread.Sleep(Timeout);
                    }

                    WriteToFile(outputStream, buffer);
                }
            }
        }

        private int ReadFromFile(Stream inputStream, out byte[] buffer)
        {
            lock (inputLock)
            {
                buffer = new byte[MegaByteArrayLength];

                try
                {
                    if (IsCanceled)
                        return EmptyBlockValue;

                    var result = inputStream.Read(buffer);
                    buffer = buffer?.Where(i => i != NullValueByte).ToArray();

                    if (!buffer.Any())
                        return EmptyBlockValue;
                }
                catch (IOException exception)
                {
                    cancellationTokenSource.Cancel();
                    logger.Error($"An error occured while reading the file. {exception.Message}");
                }
                catch (Exception exception)
                {
                    cancellationTokenSource.Cancel();
                    logger.Error($"A generic exception occured while reading the file. {exception.Message}");
                }

                ReadBlockIndex++;
                return ReadBlockIndex;
            }
        }

        private int WriteToFile(Stream gZipStream, byte[] buffer)
        {
            lock (outputLock)
            {
                try
                {
                    if (IsCanceled)
                        return EmptyBlockValue;

                    if (gZipStream.CanWrite)
                        gZipStream.Write(buffer);
                }
                catch(IOException exception)
                {
                    cancellationTokenSource.Cancel();
                    logger.Error($"An error occured while writing to the file. {exception.Message}");
                }
                catch(Exception exception)
                {
                    cancellationTokenSource.Cancel();
                    logger.Error($"A generic exception occured while writing to the file. {exception.Message}");
                }

                WriteBlockIndex++;
                return WriteBlockIndex;
            }
        }
    }
}
