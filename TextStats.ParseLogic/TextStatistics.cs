using TextStats.ParseLogic.Model;

namespace TextStats.ParseLogic
{
    public class TextStatistics
    {
        const string TextFileStatistics_FilePath = "TextFileStatistics.xml";
        public string? FilePath { get; private set; }
        public TextFile? TextFile { get; private set; }

        public async Task<ResponseData<TextFile>> ProcessFileFromURL(string url)
        {
            string fileName = System.IO.Path.GetFileName(url);
            var filePath = await Utility.DownloadFile(url);
            var checkSum = await Utility.GetFileCheckSum(filePath);
            
            return ProcessFile(filePath);
        }


        public async Task<ResponseData<TextFile>> ProcessFileFromDisk(string filePath)
        {
            FilePath = filePath;
            var checkSum = await Utility.GetFileCheckSum(filePath);
            return ProcessFile(filePath);
        }

        public ResponseData<TextFile> ProcessFile(string filePath)
        {
            FilePath = filePath;
            return new ResponseData<TextFile>();
        }


        private void ParseFile()
        { 
        }

        public List<WordFrequency> TopWords(int n)
        {
            return new List<WordFrequency>();
        }

        public List<String> LongestWords(int n)
        {
            return new List<String>();
        }

        private List<TextFile> GetSavedStatistics()
        {
            return Utility.DeSerialize<List<TextFile>>(TextFileStatistics_FilePath);
        }

        private void SaveStatistics(List<TextFile> statistics)
        {
            Utility.Serialize(statistics, TextFileStatistics_FilePath);
        }
    }
}

