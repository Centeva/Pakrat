using CommandLine;

namespace Packrat
{
    public class Options
    {
        [VerbOption("pack", HelpText = "Remove duplicate files.")]
        public PathOptions PackVerb { get; set; }

        [VerbOption("unpack", HelpText = "Restore duplicate files.")]
        public PathOptions UnpackVerb { get; set; }
    }

    public class PathOptions
    {
        [ValueOption(0)]
        public string Folder { get; set; }
    }
}