using System;
using System.IO;


namespace GZipTest
{
    class ArgsAnalyzer
    {
        /// <summary>
        /// this class used for checking parametrs entered in CMD
        /// </summary>
        public static void ArgsValidation(string[] args)
        {
            if (args.Length != 3)
            {
                throw new Exception("Please enter arguments up to the following pattern:\n [\"compress\"/\"decompress\"] [Source file] [Destination file].");
            }

            if (args[0].ToLower() != "compress" && args[0].ToLower() != "decompress")
            {
                throw new Exception("First argument should be \"compress\"c or \"decompress\"c!");
            }

            if (args[1].Length == 0)
            {
                throw new Exception("No source file name was specified.");
            }

            if (args[2].Length == 0)
            {
                throw new Exception("No destination file name was specified.");
            }

            if (!File.Exists(args[1]))
            {
                throw new Exception("No source file was found.");
            }

            if (args[1] == args[2])
            {
                throw new Exception("Source and destination files shall be different.");
            }
            FileInfo _fileIn = new FileInfo(args[1]);
            FileInfo _fileOut = new FileInfo(args[2]);
            if (_fileIn.Extension == ".gz" && args[0] == "compress")
            {
                throw new Exception("File has already been compressed.");
            }

            if (_fileOut.Extension == "decompress" && _fileOut.Exists)
            {
                throw new Exception("Destination file already exists. Please indiciate the different file name.");
            }

            if (_fileIn.Extension != ".gz" && args[0] == "decompress")
            {
                throw new Exception("File to be decompressed should have .gz extension.");
            }

            if (_fileOut.Extension != ".gz" && args[0] == "compress")
            {
                throw new Exception("Destanation File should have .gz extension.");
            }
        }
    }
}

