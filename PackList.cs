using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Packrat
{
    public class PackList
    {
        public string BasePath { get; private set; }
        public List<string> Files { get; private set; }

        private PackList(string basePath, string[] includes)
        {
            BasePath = basePath;
            Files = BuildTree(basePath, includes);
        }

        public static PackList FromDirectory(string directory)
        {
            return new PackList(directory, new [] {"**/*.*"});
        }

        public static PackList FromPackFile(string packList)
        {
            return new PackList(Path.GetDirectoryName(packList), File.ReadAllLines(packList));
        }

        private List<string> BuildTree(string basePath, string[] includes)
        {
            List<string> files = new List<string>();
            foreach (var include in includes)
            {
                var expressionParts = include.Split('/', '\\');
                var searchPattern = expressionParts.Last();
                var paths = new string[expressionParts.Length - 1];
                Array.Copy(expressionParts, paths, expressionParts.Length - 1);

                files.AddRange(ParseParts(basePath, searchPattern, paths, 0));
            }

            return files;
        }

        private IEnumerable<string> ParseParts(string currentDirectory, string searchPattern, string[] parts, int startIndex)
        {
            IEnumerable<string> files;

            if (parts.Length == 0 || startIndex >= parts.Length)
            {
                files = Directory.EnumerateFiles(currentDirectory, searchPattern, SearchOption.TopDirectoryOnly);
            }
            else if (parts[startIndex] == "**")
            {
                files = Directory.EnumerateFiles(currentDirectory, searchPattern, SearchOption.AllDirectories);
            }
            else
            {
                files = ParseParts(Path.Combine(currentDirectory, parts[startIndex]), searchPattern, parts, startIndex + 1);
            }
            
            foreach (var file in files)
            {
                yield return file;
            }
        }
    }
}