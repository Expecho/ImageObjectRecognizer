using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMetadataUpdater.Models;
using ImageMetadataUpdater.Writers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageMetadataUpdater.Services
{
    internal class PlinqService : IHostedService
    {
        private readonly ILogger<PlinqService> _logger;
        private readonly IOptions<Configuration> _configuration;
        private readonly IResultWriter _resultWriter;
        private int _queuedFiles;

        public PlinqService(ILogger<PlinqService> logger, IOptions<Configuration> configuration, IResultWriter resultWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _resultWriter = resultWriter;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var recognizer = new Recognizer(_logger, _configuration);

            Directory
                .EnumerateFiles(_configuration.Value.Path, "*.jpg", SearchOption.TopDirectoryOnly)
                .AsParallel()
                .WithDegreeOfParallelism(10)
                .ForAll(file => CreateWorker(new Input(file, ++_queuedFiles)).GetAwaiter().GetResult());

            async Task CreateWorker(Input input)
            {
                _logger.LogInformation($"Queued {input.FilePath} ({_queuedFiles})");
                var result = await recognizer.RecognizeAsync(input);
                await _resultWriter.PersistResultAsync(result);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
