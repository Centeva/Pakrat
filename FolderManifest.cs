﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Packrat
{
    public class FolderManifest : Dictionary<string, List<string>>
    {
        // Constructor for JSON deserializer
        public FolderManifest()
        {
        }

        // Constructor to use when creating a new manifest
        public FolderManifest(PackList packList)
        {
            ComputeHashes(packList.BasePath, packList.Files);
        }

        private void ComputeHashes(string basePath, IEnumerable<string> files)
        {
            var groupedHashes = files.AsParallel()
                .Select(file => new Tuple<string, string>(HashFile(file), file.RelativeTo(basePath)))
                .GroupBy(t => t.Item1, t => t.Item2);

            foreach (var entry in groupedHashes)
            {
                Add(entry.Key, new List<string>(entry));
            }
        }
        
        private static string HashFile(string path)
        {
            var hasher = SHA1.Create();
            hasher.Initialize();

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hashBytes = hasher.ComputeHash(stream);
                return hashBytes.Aggregate("", (s, b) => s + b.ToString("X2"));
            }
        }
    }
}