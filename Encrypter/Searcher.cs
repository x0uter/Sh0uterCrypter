using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Encrypter
{
    class Searcher
    {
        private List<DirectoryInfo> allDirectories;
        private List<FileInfo> allFiles;
        private string dir;

        public Searcher(string dir, CancellationToken ct)
        {
            this.dir = dir;
            this.allDirectories = new List<DirectoryInfo>();
            this.allFiles = new List<FileInfo>();
            allDirectories.Add(new DirectoryInfo(dir));
        }

        public void getSubDirectories(CancellationToken ct)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            try
            {
                Parallel.ForEach(dirInfo.EnumerateDirectories(), new ParallelOptions() { CancellationToken = ct },
                    (dirI) => {
                        dir = dirI.FullName;
                        getSubDirectories(ct);
                    });
            }
            catch (OperationCanceledException)
            {
                return;
            }
            lock (allDirectories)
            {
                allDirectories.AddRange(dirInfo.EnumerateDirectories());
            }
        }

        public void getFilesInDirectory(string ext, CancellationToken ct)
        {
            try
            {
                Parallel.ForEach(allDirectories, new ParallelOptions() { CancellationToken = ct },
                    (dirInfo) =>
                        Parallel.ForEach(dirInfo.EnumerateFiles(), new ParallelOptions() { CancellationToken = ct },
                            (file) => {
                                if (!file.Extension.Equals(ext))
                                {
                                    lock (allFiles)
                                    {
                                        allFiles.Add(file);
                                    }
                                }
                            }
                        )
                );
            }
            catch (OperationCanceledException) { }
        }

        public void printAllFiles()
        {
            foreach(FileInfo file in allFiles)
            {
                Console.WriteLine(file.Name);
            }
        }

        public List<FileInfo> getAllFiles()
        {
            return allFiles;
        }
    }
}