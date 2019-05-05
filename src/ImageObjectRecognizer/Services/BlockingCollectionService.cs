using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageObjectRecognizer.Models;
using ImageObjectRecognizer.Writers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageObjectRecognizer.Services
{
    internal class BlockingCollectionService : IHostedService
    {
        private readonly ILogger<BlockingCollectionService> _logger;
        private readonly IOptions<Configuration> _configuration;
        private readonly IResultWriter _resultWriter;
        private int _queuedFiles;
        private Recognizer _recognizer;

        public BlockingCollectionService(ILogger<BlockingCollectionService> logger, IOptions<Configuration> configuration, IResultWriter resultWriter)
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

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
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

            try
            {
                var result = await _recognizer.RecognizeAsync(input);

                await _resultWriter.PersistResultAsync(result);

                _logger.LogInformation($"Transformed {result.Input.FilePath} ({result.Input.FileIndex}).");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error processing {input.FilePath} ({input.FileIndex}): {e.Message}");
            }
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
            _logger.LogInformation($"Total # files queued: {_queuedFiles}");
        }
    }
}