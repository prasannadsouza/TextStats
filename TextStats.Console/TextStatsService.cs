using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextStats.ParseLogic;

namespace TextStats.Console
{
    internal class TextStatsService:IHostedService
    {
        private ILogger _logger;
        private IServiceProvider _serviceProvider;
        public TextStatsService(IServiceProvider serviceProvider)        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<TextStatsService>>();
        }


        private string GetOptions(List<string>? validOptions, string promptMessage)
        {
            while (true)
            {
                System.Console.WriteLine(promptMessage);
                var inputText = System.Console.ReadLine()?.Trim();

                if (validOptions?.Count > 0 == false)
                {
                    if (string.IsNullOrWhiteSpace(inputText)) continue;
                    return inputText;
                }

                var selectedOption = validOptions?.FirstOrDefault(e => e.Length == inputText!.Length && string.Compare(e, inputText, true) == 0);
                if (string.IsNullOrWhiteSpace(selectedOption) == false) return selectedOption;

                System.Console.WriteLine($"Invalid Options Selected, Valida options are {string.Join(",", validOptions!)}");
            }
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"TextStatsService is starting.");

            cancellationToken.Register(() => _logger.LogInformation($" TextStatsService task is stopping."));

            while (!cancellationToken.IsCancellationRequested)
            {
                var textStatLogic = new ParseLogic.TextStatistics(_serviceProvider);
                //var text = $"'Oh, you can't help that,' said the Cat: 'we're all mad here. I'm mad. You're mad.'";
                //text += Environment.NewLine + text;  
                //var words = textStatLogic.GetWords(text);
                
                var filePath = GetOptions(null, "Please provide a filepath local or url");
                var usePreviousStats = GetOptions(new List<string> {"Y","N"}, "Resuse Previous Statistics Type Y or N");

                bool reuseStatistics = true;
                if (usePreviousStats == "N") reuseStatistics = false;

                var sbReadOptions = new StringBuilder();
                sbReadOptions.AppendLine("1. BufferedStream");
                sbReadOptions.AppendLine("2. BufferedStreamReadLine");
                sbReadOptions.AppendLine("3. TextReadLine");

                var readOptions = GetOptions(new List<string> { "1", "2", "3" }, sbReadOptions.ToString());

                var readType = ReadFileType.BufferedStream;
                if (readOptions == "2") readType = ReadFileType.BufferedStreamReadLine;
                if (readOptions == "3") readType = ReadFileType.TextReadLine;


                //var filePath = "https://www.gutenberg.org/files/15540/15540.txt";
                try
                {
                    var fileName = Path.GetFileName(filePath);
                    var responseData = await textStatLogic.ProcessFile(filePath!, reuseStatistics, readType);
                    if (string.IsNullOrWhiteSpace(responseData.Error) == false)
                    {
                        _logger.LogError($"An Error Occurred Processing ReadType: {readType:G}, UsePreviousStatistics: {usePreviousStats}, file: {fileName}, Error:{responseData.Error}");
                    }
                    else
                    {
                        _logger.LogInformation($"Processed with ReadType: {readType:G}, UsePreviousStatistics: {usePreviousStats}, file: {fileName}" +
                            $",TotalWords:{responseData?.Data?.WordFrequencies?.Count}, TotalLines:{responseData?.Data?.NumberOfLines}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An Error Occurred Processing file {0}", filePath);
                }
            }

            _logger.LogInformation($"TextStatsService task is completed.");

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
