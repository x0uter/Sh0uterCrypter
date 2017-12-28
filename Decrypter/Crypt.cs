using Encrypter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Decrypter
{
    class Crypt
    {
        /**
         *  Directory/SubDirectories path to search encrypted files
         *  You could change it to @"C:\example"
         */
        private static readonly string PATH = Directory.GetCurrentDirectory();

        /**
         *  Files extension to not decrypt
         */
        private static readonly string NOT_ENCRYPT_EXT = ".exe";

        private static readonly string PASSWORD = "example";
        static void Main(string[] args)
        {
            Task.Factory.StartNew(() => decryptWorker());
            Console.ReadKey();
        }

        static void decryptWorker()
        {
            CancellationToken ct = new CancellationToken();
            Searcher searcher = new Searcher(PATH, ct);
            searcher.getSubDirectories(ct);
            searcher.getFilesInDirectory(NOT_ENCRYPT_EXT, ct);
            Console.WriteLine("Decrypted Files:");
            searcher.printAllFiles();
            Decrypt dec = new Decrypt(PASSWORD);
            dec.allFiles(searcher.getAllFiles(), ct);
        }
    }
}
