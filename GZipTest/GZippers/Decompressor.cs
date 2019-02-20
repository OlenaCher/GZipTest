using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipTest
{
    class Decompressor : GZipper
    {
        private static readonly object Locker = new object();

        public Decompressor(string inFileName, string outFileName) : base(inFileName, outFileName)
        {
        }

        //read compressed data chunks from disk
        public override void Start()
        {
            StartTime = DateTime.Now;
            try
            {
                using (var inFileStream = new FileStream(InFileName, FileMode.Open))
                {
                    InFileStream = inFileStream;
                    var header = new byte[8];
                    for (ChunkCounter = 0; inFileStream.Length - inFileStream.Position > 0; ChunkCounter++)
                    {
                        if (StopRequested)
                            return;
                        //ram overload - wait
                        while ((float)CompInfo.AvailablePhysicalMemory / InitialFreeRam < 0.1)
                        {
                            //force gc cleanup
                            GC.Collect(2, GCCollectionMode.Forced);
                            Thread.Sleep(1000);
                        }
                        //extract compressed data chunk size from gzip header
                        inFileStream.Read(header, 0, 8);
                        var dataChunkSize = BitConverter.ToInt32(header, 4);
                        var compressedChunk = new byte[dataChunkSize];
                        header.CopyTo(compressedChunk, 0);
                        //get the rest bytes of data block
                        inFileStream.Read(compressedChunk, 8, dataChunkSize - 9);
                        lock (Locker)
                            IncomingChunks.Add(ChunkCounter, compressedChunk);
                        var chunkNumber = ChunkCounter;
                        var decompressThread = new Thread(() => ProcessChunk(chunkNumber));
                        decompressThread.Start();
                    }
                    IncomingFinished = true;
                }
            }
            catch (Exception ex)
            {
                StopRequested = true;
                Console.Write("There is tecompression process:{0}", ex.Message);
            }
        }

        //decompress data chunkhe problem at the d
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
                using (var inMemStream = new MemoryStream(chunk.Value, 0, chunk.Value.Length))
                {
                    //stream for processed data
                    using (var outMemStream = new MemoryStream())
                    {
                        using (var zipStream = new GZipStream(inMemStream, CompressionMode.Decompress))
                        {
                            zipStream.CopyTo(outMemStream);
                        }
                        var bytes = outMemStream.ToArray();
                        lock (Locker)
                        {
                            OutgoingChunks.Add(chunkNumber, bytes);
                            IncomingChunks.Remove(chunkNumber);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StopRequested = true;
                Console.Write("There is the problem at the decompression process:{0}", ex.Message);
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
                    //write all decompressed data chunks to disk
                    for (var chunkNumber = 0; !(IncomingFinished && chunkNumber == ChunkCounter); chunkNumber++)
                    {
                        if (StopRequested)
                            return;
                        //waiting for the next chunk to write to disk
                        while(true)
                        {
                            if (StopRequested)
                                return;
                            KeyValuePair<int, byte[]> chunk;
                            lock (Locker)
                                chunk = OutgoingChunks.FirstOrDefault(b => b.Key == chunkNumber);
                            if (chunk.Value != null)
                            {
                                outFileStream.Write(chunk.Value, 0, chunk.Value.Length);
                                lock (Locker)
                                    OutgoingChunks.Remove(chunk.Key);
                                //force gc cleanup of processed bytes
                                GC.Collect(2, GCCollectionMode.Forced);
                                break;
                            }
                            Thread.Sleep(100);
                        }
                    }
                    var elapsed = DateTime.Now - StartTime;
                    Console.Write("Decompression was completed. Elapsed time is {0}ms", elapsed);
                }
            }
            catch (Exception ex)
            {
                StopRequested = true;
                Console.Write("There is the problem at the decompression process:{0}", ex.Message);
            }
        }
    }
}
