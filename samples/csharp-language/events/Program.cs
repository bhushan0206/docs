﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventSampleCode
{
    class Program
    {
        static void Main(string[] args)
        {
            var lister = new FileSearcher();
            int filesFound = 0;

            EventHandler<FileFoundArgs> handler = (sender, eventArgs) =>
            {
                Console.WriteLine(eventArgs.FoundFile);
                filesFound++;
                //eventArgs.CancelRequested = true;
            };

            lister.OnFoundFile += handler;

            lister.OnChangeDirectory += (sender, eventArgs) =>
            {
                Console.Write($"Entering '{eventArgs.CurrentSearchDirectory}'.");
                Console.WriteLine($" {eventArgs.CompletedDirs} of {eventArgs.TotalDirs} completed...");
            };

            lister.Search(".", "*.exe", true);

            lister.OnFoundFile -= handler;
        }
    }

    public class FileFoundArgs : EventArgs
    {
        public string FoundFile { get; }
        public bool CancelRequested { get; set;}

        public FileFoundArgs(string fileName)
        {
            FoundFile = fileName;
        }
    }

    internal class SearchDirectoryArgs : EventArgs
    {
        internal string CurrentSearchDirectory { get; }
        internal int TotalDirs { get; }
        internal int CompletedDirs { get; }

        internal SearchDirectoryArgs(string dir, int totalDirs, int completedDirs)
        {
            CurrentSearchDirectory = dir;
            TotalDirs = totalDirs;
            CompletedDirs = completedDirs;
        }
    }
    public class FileSearcher
    {
        public event EventHandler<FileFoundArgs> OnFoundFile;
        internal event EventHandler<SearchDirectoryArgs> OnChangeDirectory
        {
            add { changeDirectory += value; }
            remove { changeDirectory -= value; }
        }
        private event EventHandler<SearchDirectoryArgs> changeDirectory;

        public void Search(string directory, string searchPattern, bool searchSubDirs = false)
        {
            if (searchSubDirs)
            {
                var allDirectories = Directory.GetDirectories(directory, "*.*", SearchOption.AllDirectories);
                var completedDirs = 0;
                var totalDirs = allDirectories.Length + 1;
                foreach (var dir in allDirectories)
                {
                    changeDirectory?.Invoke(this,
                        new SearchDirectoryArgs(dir, totalDirs, completedDirs++));
                    // Recursively search this child directory:
                    SearchDirectory(dir, searchPattern);
                }
                // Include the Current Directory:
                changeDirectory?.Invoke(this,
                    new SearchDirectoryArgs(directory, totalDirs, completedDirs++));
                SearchDirectory(directory, searchPattern);
            }
            else
            {
                SearchDirectory(directory, searchPattern);
            }
        }

        private void SearchDirectory(string directory, string searchPattern)
        {
            foreach (var file in Directory.EnumerateFiles(directory, searchPattern))
            {
                var args = new FileFoundArgs(file);
                OnFoundFile?.Invoke(this, args);
                if (args.CancelRequested)
                    break;
            }
        }
    }
}
