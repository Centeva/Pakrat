using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;

namespace Packrat
{
    public class Program
    {
        static int Main(string[] args)
        {
            var parsedArgs = ParseArgs(args);
            if (parsedArgs == null)
            {
                return 1;
            }

            if (!Directory.Exists(parsedArgs.Folder))
            {
                Console.WriteLine("The specified folder does not exist");
                return 2;
            }

            if (string.Equals(parsedArgs.Verb, "pack", StringComparison.OrdinalIgnoreCase))
            {
                Pack(parsedArgs.Folder);
            }
            else if (string.Equals(parsedArgs.Verb, "unpack", StringComparison.OrdinalIgnoreCase))
            {
                Unpack(parsedArgs.Folder);
            }
            else
            {
                Console.WriteLine("first argument must be either pack or unpack");
                return 3;
            }

            return 0;
        }

        private static Args ParseArgs(string[] args)
        {
            string verb = null;
            string folder = null;

            try
            {
                bool parseSuccess = Parser.Default.ParseArguments(
                    args,
                    new Options(),
                    (v, o) =>
                    {
                        verb = v;
                        if (o != null)
                        {
                            folder = ((PathOptions) o).Folder;
                        }
                    });

                if (!parseSuccess)
                {
                    PrintUsage();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }

            return new Args(verb, folder);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: packrat [pack|unpack] [folder]");
        }

        private static void Pack(string folder)
        {
            FolderManifest manifest = new FolderManifest(folder);
            Parallel.ForEach(manifest, entry =>
            {
                var src = AbsolutePath(folder, entry.Value[0]);
                var dst = AbsolutePath(folder, entry.Key);
                File.Copy(src, dst, true);

                foreach (var dupe in entry.Value)
                {
                    File.Delete(AbsolutePath(folder, dupe));
                }
            });

            // Now that all the files have been moved out, removed folders that got left behind
            foreach (var orphanFolder in Directory.GetDirectories(folder))
            {
                Directory.Delete(orphanFolder, true);
            }

            var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(AbsolutePath(folder, "Manifest.json"), json, Encoding.UTF8);
        }

        private static void Unpack(string folder)
        {
            var manifestPath = AbsolutePath(folder, "Manifest.json");

            if (!File.Exists(manifestPath))
            {
                Console.WriteLine("Could not find manifest file. Assuming that this folder is not packed.");
                return;
            }

            var json = File.ReadAllText(manifestPath);
            var manifest = JsonConvert.DeserializeObject<FolderManifest>(json);

            Parallel.ForEach(manifest, entry =>
            {
                var srcFile = AbsolutePath(folder, entry.Key);
                foreach (var target in entry.Value)
                {
                    var dstFile = AbsolutePath(folder, target);
                    var dstFolder = Path.GetDirectoryName(dstFile) ?? folder;
                    if (!Directory.Exists(dstFolder))
                    {
                        Directory.CreateDirectory(dstFolder);
                    }
                    File.Copy(srcFile, dstFile, true);
                }
                File.Delete(srcFile);
            });

            File.Delete(manifestPath);
        }

        private static string AbsolutePath(string folder, string file)
        {
            return Path.Combine(folder, file);
        }

        private class Args
        {
            public Args(string verb, string folder)
            {
                Verb = verb;
                Folder = folder;
            }

            public string Verb { get; }
            public string Folder { get; }
        }
    }
}