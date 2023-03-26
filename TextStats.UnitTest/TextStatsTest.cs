using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using System;
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

            if (File.Exists(testFilePath) == false)
            {
                _logger.LogInformation("Creating Text File");
                using (var sr = File.CreateText(testFilePath))
                {
                    var sampleText = $"'Oh, you can't help that,' said the Cat: 'we're all mad here. I'm mad. You're mad.'";
                    sr.WriteLine(sampleText);
                    sr.WriteLine(sampleText);
                    sr.WriteLine(sampleText);
                    sr.Close();
                }
            }
        }

        [Test]
        public async Task TestBufferedStream()
        {
            var textStatLogic = new ParseLogic.TextStatistics(app.Services);
            var responseData = await textStatLogic.ProcessFile(new FileInfo(testFilePath).FullName, false, ParseLogic.ReadFileType.BufferedStream);
            Assert.IsTrue(string.IsNullOrWhiteSpace(responseData.Error));
            Assert.IsTrue(responseData?.Data?.NumberOfLines == 3);
            Assert.IsTrue(responseData?.Data?.NumberOfWords == 14);
            Assert.IsTrue(responseData?.Data?.WordFrequencies?.FirstOrDefault(e => e.Word == "mad")?.Frequency == 9);
        }
    }
}