﻿using System;
using System.IO;


namespace GZipTest
{
    static class ArgsAnalyzer
    {
        /// <summary>
        ///Class ArgsAnalyzer is used for checking input arguments of the command line  
        /// </summary>
        public static void InitialValidation(string[] args)
        {
            СheckArgs(args);
            string _command=args[0].ToLower();
            FileInfo _fileIn = new FileInfo(args[1]);
            FileInfo _fileOut = new FileInfo(args[2]);
            СheckFiles(_command, _fileIn, _fileOut);
            CheckDrives(_fileIn, _fileOut);
        }

        // common argument's checking
        private static void СheckArgs(string[] args)
        {
            if (args.Length != 3)
            {
                throw new Exception("Please enter arguments up to the following pattern:\n [\"compress\"/\"decompress\"] [Source file] [Destination file].");
            }
            if (args[0].ToLower() != "compress" && args[0].ToLower() != "decompress")
            {
                throw new Exception("First argument should be \"compress\"c or \"decompress\"c!");
            }
            if (args[1] == args[2])
            {
                throw new Exception("Source and destination files should be different.");
            }
        }

        //input and output files' analizing
        private static void СheckFiles(string command, FileInfo fileIn, FileInfo fileOut)
        {
            if (!fileIn.Exists)
            {
                throw new Exception("No source file was found.");
            }
            if (fileOut.Exists)
            {
                throw new Exception("Destination file has already exist. Please use the different file name.");
            } 
            if (command == "compress")
            {
                if (fileIn.Extension == ".gz")
                {
                    throw new Exception("File has already been compressed.");
                }
                if (fileOut.Extension != ".gz")
                {
                    throw new Exception("Destanation File should have .gz extension.");
                }
            }
            else 
            {
                if (fileIn.Extension != ".gz")
                {
                    throw new Exception("The sourse file isn't an archive with .gz extension.");
                }
                if (fileOut.Extension == ".gz")
                {
                    throw new Exception("Result file shouldn't be an archive.");
                }          
            }
        }

        //drives' of IO files analizing
        private static void CheckDrives(FileInfo fileIn, FileInfo fileOut)
        {
            string _pathDriveIn=Path.GetPathRoot(fileIn.FullName);
            string _pathDriveOut=Path.GetPathRoot(fileOut.FullName);
            DriveInfo _driveIn=new DriveInfo(_pathDriveIn);
            DriveInfo _driveOut=new DriveInfo(_pathDriveOut);
            if (_driveOut.AvailableFreeSpace < fileIn.Length*2)
            {
                throw new Exception("Free drive space is not enough for the creating the resulting file. Please choose another destination drive or free drive "+ _pathDriveOut);                
            }
            if (!_driveIn.IsReady)
            {
                throw new Exception("The instant file's drive "+ _pathDriveIn + " is not ready.");                
            }
            if (!_driveOut.IsReady)
            {
                throw new Exception("The destination file's drive " + _pathDriveOut + " is not ready.");                
            }
        }
    }
}

