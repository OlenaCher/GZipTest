using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipTest
{
    class Compressor : GZipper
    {
        private static readonly object Locker = new object();

        public Compressor(string inFileName, string outFileName): base(inFileName, outFileName)
        {
        }

        //reading data chunks 
        public override void Start()
        {
            StartTime = DateTime.Now;
            try
            {
                using (var inFileStream = new FileStream(InFileName, FileMode.Open))
                {
                    InFileStream = inFileStream;
                    for (ChunkCounter = 0; inFileStream.Length - inFileStream.Position > 0; ChunkCounter++)
                    {
                        if (StopRequested)
                            return;
                        //check the percentage of remaining memory to initial
                        while ((float)CompInfo.AvailablePhysicalMemory / InitialFreeRam < 0.2)
                        {
                            //forcing garbage collector cleanup
                            GC.Collect(2, GCCollectionMode.Forced);
                            Thread.Sleep(1000);
                        }
                        //optimal data read chunk size based on ram and cpu cores
                        var dataChunkSize = (long)CompInfo.AvailablePhysicalMemory / CoreCount / 4;
                        //size of chunk for the last chunck
                        if (inFileStream.Length - inFileStream.Position <= dataChunkSize)
                            dataChunkSize = inFileStream.Length - inFileStream.Position;
                        //size of chunk for small files
                        if (inFileStream.Length < dataChunkSize)
                            dataChunkSize = inFileStream.Length / CoreCount + 1;
                        //constant size of chunk for huge files
                        if (dataChunkSize > 256 * 1024 * 1024)
                            dataChunkSize = 256 * 1024 * 1024;
                        var dataChunk = new byte[dataChunkSize];
                        //read file
                        inFileStream.Read(dataChunk, 0, (int)dataChunkSize);
                        lock (Locker)
                            IncomingChunks.Add(ChunkCounter, dataChunk);
                        //starting new thread with chunk number as an argument
                        var chunkNumber = ChunkCounter;
                        var compressThread = new Thread(() => ProcessChunk(chunkNumber));
                        compressThread.Start();
                    }
                    IncomingFinished = true;
                }
            }
            catch (Exception ex)
            {
                StopRequested = true;
                Console.Write("There is the problem at the compression process:{0}", ex.Message);
            }
        }

        //compress data chunk
        protected override void ProcessChunk(int chunkNumber)
        {
            if (StopRequested)
                return;
            ProcessSemaphore.WaitOne();
            try
            {
                KeyValuePair<int, byte[]> chunk;
                lock (Locker)
                    chunk = IncomingChunks.FirstOrDefault(b => b.Key == chunkNumber);
                //stream for processed data
                using (var outMemStream = new MemoryStream())
                {
                    using (var zipStream = new GZipStream(outMemStream, CompressionMode.Compress))
                    {
                        using (var inMemStream = new MemoryStream(chunk.Value, 0, chunk.Value.Length))
                        {
                            inMemStream.CopyTo(zipStream);
                        }
                    }
                    //compressed bytes
                    var bytes = outMemStream.ToArray();
                    lock (Locker)
                    {
                        //add new data chunk to collection of compressed data
                        OutgoingChunks.Add(chunkNumber, bytes);
                        //remove chunk from collection of uncompressed data
                        IncomingChunks.Remove(chunkNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                StopRequested = true;
                Console.Write("There is an exception at the compressing process: {0}", ex.Message); 
            }
            ProcessSemaphore.Release();
            //if this is the first chunk - start writing to disk in a separate thread
            if (chunkNumber == 0 && WritingThread.ThreadState != ThreadState.Running && !StopRequested)
                WritingThread.Start();
        }

        protected override void WriteToDisk()
        {
            try
            {
                using (var outFileStream = new FileStream(OutFileName, FileMode.Create))
                {
                    OutFileStream = outFileStream;
                    //write all zipped data chunks to disk
                    for (var chunkNumber = 0; !(IncomingFinished && chunkNumber == ChunkCounter); chunkNumber++)
                    {
                        if (StopRequested)
                            return;
                        //waiting for the next chunk to write to disk
                        while(true)
                        {
                            KeyValuePair<int, byte[]> chunk;
                            lock (Locker)
                                chunk = OutgoingChunks.FirstOrDefault(b => b.Key == chunkNumber);
                            if (chunk.Value != null)
                            {
                                if (StopRequested)
                                    return;
                                //append compressed data chunk size to gzip header
                                BitConverter.GetBytes(chunk.Value.Length + 1).CopyTo(chunk.Value, 4);
                                outFileStream.Write(chunk.Value, 0, chunk.Value.Length);
                                lock (Locker)
                                    OutgoingChunks.Remove(chunk.Key);
                                break;
                            }
                            Thread.Sleep(100);
                        }
                    }
                    var elapsed = DateTime.Now - StartTime;
                    Console.Write("The compession was completed. Elapsed time is {0}ms", elapsed);
                }
            }
            catch (Exception ex)
            {
                StopRequested = true;
                Console.Write("There is the problem at the compression process:{0}",ex.Message);
            }
        }
    }
}
