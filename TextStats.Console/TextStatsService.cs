using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextStats.Console
{
    internal class TextStatsService:IHostedService
    {
        public ILogger Logger { get; private set; }
        public IServiceProvider ServiceProvider { get; private set; }
        public TextStatsService(IServiceProvider serviceProvider)        {
            ServiceProvider = serviceProvider;
            Logger = serviceProvider.GetRequiredService<ILogger<TextStatsService>>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"TextStatsService is starting.");

            cancellationToken.Register(() => Logger.LogInformation($" TextStatsService task is stopping."));

            while (!cancellationToken.IsCancellationRequested)
            {
                break;
            }

            Logger.LogInformation($"TextStatsService task is completed.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
