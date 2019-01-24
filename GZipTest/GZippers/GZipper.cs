using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading;
using Microsoft.VisualBasic.Devices;

namespace GZipTest
{
    abstract class GZipper
    {
        //number of available processor cores - is the number of threads we will use
        protected static readonly int CoreCount = Environment.ProcessorCount;
        protected readonly string InFileName;
        protected readonly string OutFileName;
        //ComputerInfo will be used to get free RAM amount
        protected readonly ComputerInfo CompInfo;
        protected int ChunkCounter;
        protected DateTime StartTime;
        protected readonly Dictionary<int, byte[]> OutgoingChunks;
        protected readonly Dictionary<int, byte[]> IncomingChunks;
        protected bool IncomingFinished;
        //separate thread for writing compressed data chunks to disk
        protected readonly Thread FlushThread;
        //semaphore to control number of simultaneous compress threads
        protected static Semaphore ProcessSemaphore;
        protected bool StopRequested;
        protected FileStream InFileStream;
        protected FileStream OutFileStream;
        protected readonly ulong InitialFreeRam;


        protected GZipper(string inFileName, string outFileName)
        {
            InFileName = inFileName;
            OutFileName = outFileName;
            CompInfo = new ComputerInfo();
            OutgoingChunks = new Dictionary<int, byte[]>();
            IncomingChunks = new Dictionary<int, byte[]>();
            ProcessSemaphore = new Semaphore(CoreCount, CoreCount);
            FlushThread = new Thread(FlushToDisk);
            InitialFreeRam = CompInfo.AvailablePhysicalMemory;

            Console.CancelKeyPress += delegate
            {
                StopRequested = true;
                InFileStream?.Close();
                OutFileStream?.Close();
                Console.Write("1");
            };
        }

        public abstract void Start();

        protected abstract void ProcessChunk(int chunkNumber);

        protected abstract void FlushToDisk();
    }
}
