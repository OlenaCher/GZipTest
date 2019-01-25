using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ArgsAnalyzer.InitialValidation(args);
                switch (args[0])
                {
                    case "compress":
                        var compressor = new Compressor(args[1], args[2]);
                        compressor.Start();
                        break;
                    case "decompress":
                        var decompressor = new Decompressor(args[1], args[2]);
                        decompressor.Start();
                        break;
                    default:
                        Console.WriteLine("The first argument should be \"compress\" or \"decompress\"!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message+"\nPlease fix it and try again");
            }
        }
    }
}
