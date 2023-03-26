using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using TextStats.ParseLogic.Model;

using static System.Net.Mime.MediaTypeNames;

namespace TextStats.ParseLogic
{
    public enum ReadFileType
    {
        BufferedStream = 1,
        TextReadLine = 2,
        BufferedStreamReadLine = 3,
    }


public class TextStatistics
    {
        
        const string TextFileStatistics_FilePath = "TextFileStatistics.xml";
        const string WordFreQuency_FileName = "WordFrequency.xml";

        const string ERROR_NOFILEPROCESSED = "No File Processed, Please process file from URL or Disk";
        const string ERROR_NOWORDSINFILE = "No Words in File";
        const string ERROR_FILEPATHISINVALID = "File Path is Invalid";
        const string ERROR_FILEISNOTATEXTFILE = "File is not a text File";
        const int BUFFERSIZE = 16384;
        public TextFile? TextFile { get; private set; }

        private ILogger _logger;
        private IServiceProvider _serviceProvider;
        public TextStatistics(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<TextStatistics>>();
        }

        public async Task<ResponseData<TextFile>> ProcessFile(string filePath, bool reuseStatistics = true, ReadFileType readFileType = ReadFileType.BufferedStream)
        { 
            if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) == false) return new ResponseData<TextFile> { Error = ERROR_FILEPATHISINVALID };

            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = Guid.NewGuid().ToString();

            var localFilePath = filePath;

            var startTime = DateTime.Now;
            if (!uri.IsFile) localFilePath = await Utility.DownloadFile(uri);
            _logger.LogInformation($"TotalTime to download file: {fileName}, {(DateTime.Now - startTime).TotalMilliseconds}ms ");
            var reponseData = await ProcessFile(localFilePath, fileName, reuseStatistics, readFileType);

            _logger.LogInformation($"ProcessFile Completed for file: {fileName}");
            return reponseData;
        }

        private async Task<ResponseData<TextFile>> ProcessFile(string filePath, string fileName, bool reuseStatistics, ReadFileType readFileType)
        {
            if (Utility.IsBinary(filePath)) return new ResponseData<TextFile> { Error = ERROR_FILEISNOTATEXTFILE };
            var startTime = DateTime.Now;
            var checkSum = await Utility.GetFileCheckSum(filePath);

            if (reuseStatistics)
            {
                var savedStatistic = GetSavedStatistic(checkSum);
                if (savedStatistic != null)
                {
                    _logger.LogInformation($"ProcessFile Returning Saved Statistic for file: {fileName}");
                    TextFile = savedStatistic;
                    return new ResponseData<TextFile> { Data = TextFile };
                }
            }

            TextFile = new TextFile
            {
                CheckSum = checkSum,
                FileName = fileName,
                Guid = Guid.NewGuid(),
                WordFrequencies = new List<WordFrequency>(),
                NumberOfLines  = 0,
                NumberOfWords = 0,
            };

            switch (readFileType)
            {
                case ReadFileType.TextReadLine:
                    await ParseFileWithTextReadLine(filePath);
                    break;
                case ReadFileType.BufferedStreamReadLine:
                    await ParseFileWithBufferedStreamReadLine(filePath);
                    break;
                default:
                    await ParseFileWithBufferedStream(filePath);
                    break;

            }

            TextFile!.NumberOfWords = TextFile!.WordFrequencies!.Count;
            SaveStatistic(TextFile);
            _logger.LogInformation($"TotalTime to Complete, File: {Path.GetFileName(filePath)}, {(DateTime.Now - startTime).TotalSeconds}s ");

            return new ResponseData<TextFile> { Data = TextFile };
        }


        private async Task ParseFileWithTextReadLine(string filePath)
        {
            var startTime = DateTime.Now;
            using (var sr = File.OpenText(filePath))
            {
                var text = string.Empty;
                while ((text = await sr.ReadLineAsync()) != null)
                {
                    TextFile!.NumberOfLines += 1;
                    UpdateWordFrequencies(GetWords(text));
                    _logger.LogInformation($"Processed Lines: {TextFile!.NumberOfLines}, Words: {TextFile!.WordFrequencies!.Count}");
                }
            }

            _logger.LogInformation($"TotalTime to Parse with OpenTextReadLine, File: {Path.GetFileName(filePath)}, {(DateTime.Now - startTime).TotalSeconds}s ");
        }

        private async Task ParseFileWithBufferedStreamReadLine(string filePath)
        {
            var startTime = DateTime.Now;
            using (var fs = File.OpenRead(filePath))
            {
                using (var bs = new BufferedStream(fs))
                {
                    using (var sr = new StreamReader(bs))
                    {
                        var text = string.Empty;
                        while ((text = await sr.ReadLineAsync()) != null)
                        {
                            TextFile!.NumberOfLines += 1;
                            UpdateWordFrequencies(GetWords(text));
                            _logger.LogInformation($"Processed Lines: {TextFile!.NumberOfLines}, Words: {TextFile!.WordFrequencies!.Count}");
                        }
                    }
                }
            }
            _logger.LogInformation($"TotalTime to Parse with BufferedStreamReadLine, File: {Path.GetFileName(filePath)}, {(DateTime.Now - startTime).TotalSeconds}s ");
        }

        private async Task ParseFileWithBufferedStream(string filePath)
        {
            var lastword = string.Empty;
            var startTime = DateTime.Now;
           
            using (var fs = File.OpenRead(filePath))
            {
                using (var bs = new BufferedStream(fs, BUFFERSIZE))
                {
                    var bytesToRead = BUFFERSIZE;

                    while (bytesToRead > 0)
                    {
                        var fileContents = new byte[BUFFERSIZE];
                        bytesToRead = await bs.ReadAsync(fileContents, 0, BUFFERSIZE);
                        var text = lastword + Encoding.Default.GetString(fileContents);

                        var tempWords = GetWords(text);
                        if (tempWords.Count > 0)
                        {
                            lastword = tempWords.Last();
                            tempWords.RemoveAt(tempWords.Count - 1);
                        }
                        else
                        {
                            lastword = string.Empty;
                        }
                        TextFile!.NumberOfLines = tempWords.Count(e => e == Environment.NewLine);
                        UpdateWordFrequencies(tempWords);
                        _logger.LogInformation($"Processed {bs.Position}/{bs.Length}, Lines: {TextFile!.NumberOfLines}, Words: {TextFile!.WordFrequencies!.Count}");
                    }
                }
            }
            _logger.LogInformation($"TotalTime to Parse with BufferedStream, File: {Path.GetFileName(filePath)}, {(DateTime.Now - startTime).TotalSeconds}s ");
            
        }

        private void UpdateWordFrequencies(List<string> tempWords)
        {
            foreach (var tempWord in tempWords)
            {
                var finalWord = GetFinalWord(tempWord);
                if (string.IsNullOrWhiteSpace(finalWord)) continue;

                var wordFrequency = TextFile?.WordFrequencies?.FirstOrDefault(e => e.Word!.Length == finalWord.Length && string.Compare(e.Word, finalWord, true) == 0);
                if (wordFrequency == null)
                {
                    TextFile!.WordFrequencies!.Add(new WordFrequency { Word = finalWord, Frequency = 1 });
                }
                else
                {
                    wordFrequency.Frequency += 1;
                }
            }
        }

        public List<string> GetWords(string text)
        {
            var words = new List<string>();
            var sbCurrentWord = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                sbCurrentWord.Append(text[i]);
                if (char.IsWhiteSpace(text[i]))
                {
                    words.Add(sbCurrentWord.ToString());
                    sbCurrentWord.Clear();
                }
            }
           
            words.Add(sbCurrentWord.ToString());
            return words;
        }

        private string? GetFinalWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return null;

            var sbWord = new StringBuilder(word.Trim());
            if (sbWord.Length > 0 == false) return null;

            while (sbWord.Length > 0)
            {
                if (char.IsPunctuation(sbWord[0]))
                {
                    sbWord.Remove(0, 1);
                    continue;
                }
                break;
            }

            while (sbWord.Length > 0)
            {
                if (char.IsPunctuation(sbWord[sbWord.Length - 1]))
                {
                    sbWord.Remove(sbWord.Length - 1, 1);
                    continue;
                }
                break;
            }

            var finalWord = sbWord.ToString();
            if (double.TryParse(finalWord, System.Globalization.CultureInfo.InvariantCulture, out _ ) == true) return null;
            if (double.TryParse(finalWord, out _) == true) return null;

            return finalWord;
        }

        public ResponseData<List<WordFrequency>> TopWords(int n)
        {
            if (TextFile == null) return new ResponseData<List<WordFrequency>> { Error = ERROR_NOFILEPROCESSED };
            if (TextFile?.WordFrequencies?.Count > 0 == false) return new ResponseData<List<WordFrequency>> { Error = ERROR_NOWORDSINFILE };

            var data = TextFile!.WordFrequencies!.OrderByDescending(e => e.Frequency).Take(n).ToList();
            return new ResponseData<List<WordFrequency>> { Data = data };
        }

        public ResponseData<List<string>> LongestWords(int n)
        {
            if (TextFile == null) return new ResponseData<List<string>> { Error = ERROR_NOFILEPROCESSED };
            if (TextFile?.WordFrequencies?.Count > 0 == false) return new ResponseData<List<string>> { Error = ERROR_NOWORDSINFILE };

            var data = TextFile!.WordFrequencies!.OrderByDescending(e => e.Word!.Length).Take(n).Select(e => e.Word!).ToList();
            return new ResponseData<List<string>> { Data = data };
            
        }

        private List<TextFile>? GetSavedStatistics()
        {
            return Utility.DeSerialize<List<TextFile>>(Path.Combine(Utility.GetBaseFolderPath(), TextFileStatistics_FilePath));
        }

        private void SaveStatistics(List<TextFile> statistics)
        {
            Utility.Serialize(statistics, Path.Combine(Utility.GetBaseFolderPath(), TextFileStatistics_FilePath));
        }

        private TextFile? GetSavedStatistic(string checkSum)
        {
            var savedStatistic = GetSavedStatistics()?.FirstOrDefault(e => e.CheckSum == checkSum);
            if (savedStatistic == null) return null;

            if (savedStatistic.NumberOfWords > 0 == false) return savedStatistic;

            savedStatistic.WordFrequencies = Utility.DeSerialize<List<WordFrequency>>(Path.Combine(Utility.GetBaseFolderPath()
                , savedStatistic.Guid?.ToString()!,WordFreQuency_FileName));

            if (savedStatistic.WordFrequencies?.Count > 0 == false) return null;
            return savedStatistic;
        }

        private void SaveStatistic(TextFile statistic)
        {
            var statistics = GetSavedStatistics();
            if (statistics == null) statistics = new List<TextFile>();

            var wordFrequencies = statistic!.WordFrequencies;

            if (statistic!.WordFrequencies?.Count > 0) Utility.Serialize(wordFrequencies, Path.Combine(Utility.GetBaseFolderPath(), statistic!.Guid!.ToString()!, WordFreQuency_FileName));

            statistic.WordFrequencies = null;
            statistics.Add(statistic);
            SaveStatistics(statistics);
            statistic.WordFrequencies = wordFrequencies;
        }
    }
}

