namespace ImageObjectRecognizer.Models
{
    public class Input
    {
        public Input(string filePath, int fileIndex)
        {
            FilePath = filePath;
            FileIndex = fileIndex;
        }

        public string FilePath { get; private set; }
        public int FileIndex { get; private set; }
    }
}