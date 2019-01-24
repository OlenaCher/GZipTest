using System;
using System.Collections.Generic;
using System.IO;
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
                //read uncompressed data chunks from disk
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
                        //ram overload - wait
                        while ((float)CompInfo.AvailablePhysicalMemory / InitialFreeRam < 0.2)
                        {
                            //force gc cleanup
                            GC.Collect(2, GCCollectionMode.Forced);
                            Thread.Sleep(1000);
                        }
                        //optimal data read chunk size based on ram and cpu cores
                        var dataChunkSize = (long)CompInfo.AvailablePhysicalMemory / CoreCount / 4;
                        if (inFileStream.Length - inFileStream.Position <= dataChunkSize)
                            //this is the last file part
                            dataChunkSize = inFileStream.Length - inFileStream.Position;
                        if (inFileStream.Length < dataChunkSize)
                            dataChunkSize = inFileStream.Length / CoreCount + 1;
                        if (dataChunkSize > 256 * 1024 * 1024)
                            //we don't need too huge chunks
                            dataChunkSize = 256 * 1024 * 1024;
                        var dataChunk = new byte[dataChunkSize];
                        //read file
                        inFileStream.Read(dataChunk, 0, (int)dataChunkSize);
                        lock (Locker)
                            IncomingChunks.Add(ChunkCounter, dataChunk);
                        //start new thread with chunk number as argument
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
            //if this is the first chunk - start flush to disk in a separate thread
            if (chunkNumber == 0 && FlushThread.ThreadState != ThreadState.Running && !StopRequested)
                FlushThread.Start();
        }

        protected override void FlushToDisk()
        {
            try
            {
                using (var outFileStream = new FileStream(OutFileName, FileMode.Create))
                {
                    OutFileStream = outFileStream;
                    //flush all zipped data chunks to disk
                    for (var chunkNumber = 0; !(IncomingFinished && chunkNumber == ChunkCounter); chunkNumber++)
                    {
                        if (StopRequested)
                            return;
                        //waiting for the next chunk to flush to disk
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
