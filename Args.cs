namespace Packrat
{
    public class Args
    {
        public Args(Command command, string folderOrFile, string destinationFolder)
        {
            Command = command;
            FolderOrFile = folderOrFile;
            DestinationFolder = destinationFolder;
        }

        public Command Command { get; }
        public string FolderOrFile { get; }
        public string DestinationFolder { get; set; }

        public static Args Invalid()
        {
            return new Args(Command.Invalid, null, null);
        }
    }
}