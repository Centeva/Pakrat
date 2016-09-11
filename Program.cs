using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Packrat
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var parsedArgs = ParseArgs(args);
            int exitCode;

            switch (parsedArgs.Command)
            {
                case Command.Pack:
                    if (ValidateFolder(parsedArgs, out exitCode))
                    {
                        Pack(PackList.FromDirectory(parsedArgs.FolderOrFile), parsedArgs.DestinationFolder);
                    }
                    break;
                case Command.PackList:
                    if (ValidateFile(parsedArgs, out exitCode))
                    {
                        Pack(PackList.FromPackFile(parsedArgs.FolderOrFile), parsedArgs.DestinationFolder);
                    }
                    break;
                case Command.Unpack:
                    if (ValidateFolder(parsedArgs, out exitCode))
                    {
                        Unpack(parsedArgs.FolderOrFile);
                    }
                    break;
                case Command.UnpackList:
                    if (ValidateFile(parsedArgs, out exitCode))
                    {
                        Unpack(parsedArgs.FolderOrFile);
                    }
                    break;
                default:
                    PrintUsage();
                    exitCode = 1;
                    break;

            }
            return exitCode;
        }

        private static bool ValidateFolder(Args parsedArgs, out int exitCode)
        {
            if (!Directory.Exists(parsedArgs.FolderOrFile))
            {
                Console.WriteLine("The specified folder does not exist");
                {
                    exitCode = 2;
                    return false;
                }
            }
            exitCode = 0;
            return true;
        }

        private static bool ValidateFile(Args parsedArgs, out int exitCode)
        {
            if (!File.Exists(parsedArgs.FolderOrFile))
            {
                Console.WriteLine("The specified file does not exist");
                {
                    exitCode = 3;
                    return false;
                }
            }
            exitCode = 0;
            return true;
        }

        private static Args ParseArgs(string[] args)
        {
            if (args.Length == 0 || args.Length > 3)
            {
                return Args.Invalid();
            }

            string command = args[0];

            // TODO: Handle both absolute and relative paths
            string directory = args.Length == 1 ? Environment.CurrentDirectory : args[1];
            string file = Path.Combine(args.Length == 1 ? Environment.CurrentDirectory : args[1]);
            string destination = args.Length == 2 ? Environment.CurrentDirectory : args[2];

            switch (command.ToLowerInvariant())
            {
                case "pack":
                    return new Args(Command.Pack, directory, destination);
                case "packlist":
                    return new Args(Command.PackList, file, destination);
                case "unpack":
                    return new Args(Command.Unpack, directory, destination);
                case "unpacklist":
                    return new Args(Command.UnpackList, file, destination);
                default:
                    return Args.Invalid();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  packrat pack <folder> [destination]");
            Console.WriteLine("  packrat unpack <folder> [destination]");
            Console.WriteLine("  packrat packlist <packfile> <destination>");
            Console.WriteLine("  packrat unpacklist <folder> <destination>");
            Console.WriteLine("Notes:");
            Console.WriteLine("  - If the destination folder is not specified it will default to the current directory");
            Console.WriteLine("  - If the destination folder is specified and is different than the source directory all files in the destination will be automatically deleted when packing");
        }

        private static void Pack(PackList packList, string destinationFolder)
        {
            string sourceFolder = packList.BasePath.EndWithPathSep();
            destinationFolder = destinationFolder.EndWithPathSep();
            bool isInPlacePack = string.Equals(sourceFolder, destinationFolder, StringComparison.OrdinalIgnoreCase);
            string manifestPath = Path.Combine(destinationFolder, "Manifest.json");

            if (File.Exists(manifestPath) && isInPlacePack)
            {
                Console.WriteLine("Nothing to do since it looks like this folder is already packed.");
                return; // Don't allow re-packing of already packed folder
            }
            else if (!isInPlacePack && Directory.Exists(destinationFolder))
            {
                Directory.Delete(destinationFolder, recursive: true);
                // Testing has shown that a create directory immediately after the above delete doesn't always work because the file system 
                // reports that it's still there.  Waiting for the directory reports that it no longer exists seems to get past this quirk.
                // This issue has only been observed while debugging so far.
                while (Directory.Exists(destinationFolder))
                {
                    Console.WriteLine("Waiting for destination folder to be deleted...");
                    System.Threading.Thread.Sleep(10);
                }
            }

            // Ensure that the destination directory exists -- This call should succeed if the directory already exists
            Directory.CreateDirectory(destinationFolder);

            FolderManifest manifest = new FolderManifest(packList);
            Parallel.ForEach(manifest, entry =>
            {
                var src = entry.Value.First().ToAbsoluteFrom(sourceFolder);
                var dst = entry.Key.ToAbsoluteFrom(destinationFolder);
                if (src != dst)
                {
                    File.Copy(src, dst, overwrite: true);

                    // Only delete if we are doing an in-place pack
                    if (isInPlacePack)
                    {
                        foreach (var dupe in entry.Value)
                        {
                            File.Delete(dupe.ToAbsoluteFrom(sourceFolder));
                        }
                    }
                }
            });

            // Now that all the files have been moved out, removed folders that got left behind
            foreach (var orphanFolder in Directory.EnumerateDirectories(sourceFolder, "*.*", SearchOption.AllDirectories).Where(f => !Directory.EnumerateFiles(f, "*.*", SearchOption.AllDirectories).Any()))
            {
                Directory.Delete(orphanFolder, true);
            }

            var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(manifestPath, json, Encoding.UTF8);
        }

        private static void Unpack(string folder)
        {
            var manifestPath = "Manifest.json".ToAbsoluteFrom(folder);

            if (!File.Exists(manifestPath))
            {
                Console.WriteLine("Could not find manifest file. Assuming that this folder is not packed.");
                return;
            }

            var json = File.ReadAllText(manifestPath);
            var manifest = JsonConvert.DeserializeObject<FolderManifest>(json);

            Parallel.ForEach(manifest, entry =>
            {
                var srcFile = entry.Key.ToAbsoluteFrom(folder);
                foreach (var target in entry.Value)
                {
                    var dstFile = target.ToAbsoluteFrom(folder);
                    if (srcFile != dstFile)
                    {
                        var dstFolder = Path.GetDirectoryName(dstFile) ?? folder;
                        if (!Directory.Exists(dstFolder))
                        {
                            Directory.CreateDirectory(dstFolder);
                        }
                        File.Copy(srcFile, dstFile, true);
                    }

                }
                File.Delete(srcFile);
            });

            File.Delete(manifestPath);
        }
    }
}