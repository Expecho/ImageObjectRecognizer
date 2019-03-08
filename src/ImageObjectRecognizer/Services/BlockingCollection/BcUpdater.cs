using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMetadataUpdater.Models;
using ImageMetadataUpdater.Writers;

namespace ImageMetadataUpdater.Services.BlockingCollection
{
    internal class BcUpdater : IHostedService
    {
        private readonly ILogger<BcUpdater> _logger;
        private readonly IOptions<Configuration> _configuration;
        private readonly IResultWriter _resultWriter;
        private int _queuedFiles;
        private Recognizer _recognizer;

        public BcUpdater(ILogger<BcUpdater> logger, IOptions<Configuration> configuration, IResultWriter resultWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _resultWriter = resultWriter;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _recognizer = new Recognizer(_logger, _configuration);

            // Set up pipeline
            var pipeline = new BlockingCollection<Input>(boundedCapacity: 10);

            // Start the consumer
            var processingTask = ProcessQueuedFilesAsync(pipeline, cancellationToken);
            
            // Start producing data
            QueueImageFiles(_configuration.Value.Path, pipeline);

            // Signal producer completion
            pipeline.CompleteAdding();

            // Wait for pipeline to drain
            await processingTask;
          }

        private Task ProcessQueuedFilesAsync(BlockingCollection<Input> pipeline, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                await Task.WhenAll(pipeline.GetConsumingEnumerable(cancellationToken)
                    .Select(ProcessQueuedFileAsync));
            }, cancellationToken);
        }

        private async Task ProcessQueuedFileAsync(Input input)
        {
            _logger.LogInformation($"Transforming {input.FilePath} ({input.FileIndex}).");
            var result = await _recognizer.RecognizeAsync(input);

            await _resultWriter.PersistResultAsync(result);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void QueueImageFiles(string path, BlockingCollection<Input> target)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                target.Add(new Input(file, ++_queuedFiles));
                _logger.LogInformation($"Queued {file} ({_queuedFiles})");
            }
        }
    }
}