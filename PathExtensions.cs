using System.IO;

namespace Packrat
{
    public static class PathExtensions
    {
        public static string RelativeTo(this string path, string basePath)
        {
            basePath = EndWithPathSep(basePath);

            if (path.StartsWith(basePath))
            {
                return path.Substring(basePath.Length);
            }

            return path;
        }

        public static string ToAbsoluteFrom(this string file, string basePath)
        {
            return Path.Combine(basePath, file);
        }

        public static string EndWithPathSep(this string path)
        {
            return path.EndsWith("\\") ? path : path + "\\";
        }
    }
}