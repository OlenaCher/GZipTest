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
        //number of processor cores will be the number of compressing/decompressing threads 
        protected static readonly int CoreCount = Environment.ProcessorCount;
        protected readonly string InFileName;
        protected readonly string OutFileName;
        protected FileStream InFileStream;
        protected FileStream OutFileStream;
        protected readonly ComputerInfo CompInfo;
        //free RAM on stratring zipping process
        protected readonly ulong InitialFreeRam;
        protected int ChunkCounter;
        protected DateTime StartTime;
        protected readonly Dictionary<int, byte[]> OutgoingChunks;
        protected readonly Dictionary<int, byte[]> IncomingChunks;
        protected bool IncomingFinished;
        //thread for writing compressed data chunks to drive
        protected readonly Thread WritingThread;
        //semaphore to control number of compress threads
        protected static Semaphore ProcessSemaphore;
        protected bool StopRequested;


        protected GZipper(string inFileName, string outFileName)
        {
            InFileName = inFileName;
            OutFileName = outFileName;
            CompInfo = new ComputerInfo();
            InitialFreeRam = CompInfo.AvailablePhysicalMemory;
            OutgoingChunks = new Dictionary<int, byte[]>();
            IncomingChunks = new Dictionary<int, byte[]>();
            ProcessSemaphore = new Semaphore(CoreCount, CoreCount);
            WritingThread = new Thread(WriteToDisk);

            Console.CancelKeyPress += delegate
            {
                StopRequested = true;
                InFileStream?.Close();
                OutFileStream?.Close();
                Console.Write("Application's stop is requested");
            };
        }

        public abstract void Start();

        protected abstract void ProcessChunk(int chunkNumber);

        protected abstract void WriteToDisk();
    }
}
