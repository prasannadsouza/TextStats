using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using System;
using TextStats.ParseLogic.Model;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TextStats.UnitTest
{
    public class TextStatsTest
    {
        private const string testFilePath = "TestFile.txt" ;
        private IHost app;
        private ILogger _logger;
        [SetUp]
        public void Setup()
        {
            var builder = Host.CreateDefaultBuilder();
            app = builder.Build();
            _logger = app.Services.GetRequiredService<ILogger<TextStatsTest>>();

            _logger.LogInformation("Setting up test");

            if (File.Exists(testFilePath)) File.Delete(testFilePath);

            _logger.LogInformation("Creating Text File");
            using (var sr = File.CreateText(testFilePath))
            {
                var sampleText = $"'Oh, you can't help that,' said the Cat: 'we're all mad here. I'm mad. You're mad.'";
                for (int i = 0; i < 199; i++)
                {
                    sr.WriteLine(sampleText);
                }
                sr.Write(sampleText);
                sr.Close();
            }
        }

        [Test]
        public async Task TestBufferedStream()
        {
            var textStatLogic = new ParseLogic.TextStatistics(app.Services);
            var responseData = await textStatLogic.ProcessFile(new FileInfo(testFilePath).FullName, false, ParseLogic.ReadFileType.BufferedStream);
            ValidateResponseForSuccess(textStatLogic, responseData);
        }

        [Test]
        public async Task TestBufferedStreamReadLine()
        {
            var textStatLogic = new ParseLogic.TextStatistics(app.Services);
            var responseData = await textStatLogic.ProcessFile(new FileInfo(testFilePath).FullName, false, ParseLogic.ReadFileType.BufferedStreamReadLine);
            ValidateResponseForSuccess(textStatLogic, responseData);
        }

        [Test]
        public async Task TestTextReadLine()
        {
            var textStatLogic = new ParseLogic.TextStatistics(app.Services);
            var responseData = await textStatLogic.ProcessFile(new FileInfo(testFilePath).FullName, false, ParseLogic.ReadFileType.TextReadLine);
            ValidateResponseForSuccess(textStatLogic, responseData);
        }

        [Test]
        public async Task TestBufferedStreamReuseStatistics()
        {
            var textStatLogic = new ParseLogic.TextStatistics(app.Services);
            var responseData = await textStatLogic.ProcessFile(new FileInfo(testFilePath).FullName, true, ParseLogic.ReadFileType.BufferedStream);
            ValidateResponseForSuccess(textStatLogic, responseData);
        }

        private void ValidateResponseForSuccess(ParseLogic.TextStatistics textStatLogic, ResponseData<TextFile> responseData)
        {
            Assert.IsTrue(string.IsNullOrWhiteSpace(responseData.Error));
            Assert.That(responseData!.Data!.NumberOfLines!, Is.EqualTo(200));
            Assert.That(responseData!.Data!.NumberOfWords!, Is.EqualTo(14));
            Assert.That(responseData?.Data?.WordFrequencies?.FirstOrDefault(e => e.Word == "mad")?.Frequency, Is.EqualTo(600));
            Assert.That(textStatLogic.TopWords(1).Data?.FirstOrDefault()?.Word, Is.EqualTo("mad"));

            var longestWords = textStatLogic.LongestWords(3);
            Assert.That(longestWords.Data?.FirstOrDefault(e => e == "can't"), Is.EqualTo("can't"));
            Assert.That(longestWords.Data?.FirstOrDefault(e => e == "we're"), Is.EqualTo("we're"));
            Assert.That(longestWords.Data?.FirstOrDefault(e => e == "You're"), Is.EqualTo("You're"));
        }
    }
}