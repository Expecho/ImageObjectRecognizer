using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ImageObjectRecognizer.Models;
using ImageObjectRecognizer.Writers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageObjectRecognizer.Services
{
    internal class ChannelsService : IHostedService
    {
        private readonly ILogger<ChannelsService> _logger;
        private readonly IOptions<Configuration> _configuration;
        private readonly IResultWriter _resultWriter;
        private int _queuedFiles;
        private Recognizer _recognizer;

        public ChannelsService(ILogger<ChannelsService> logger, IOptions<Configuration> configuration, IResultWriter resultWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _resultWriter = resultWriter;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _recognizer = new Recognizer(_logger, _configuration);

            // Set up pipeline
            var pipeline = Channel.CreateBounded<Input>(new BoundedChannelOptions(10)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });

            // Start the consumer
            var processingTask = ProcessQueuedFilesAsync(pipeline.Reader, cancellationToken);

            // Start producing data
            await QueueImageFilesAsync(_configuration.Value.Path, pipeline.Writer);

            // Signal producer completion
            pipeline.Writer.Complete();

            // Wait for pipeline to drain
            await processingTask;

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
        }

        private async Task ProcessQueuedFilesAsync(ChannelReader<Input> pipeline, CancellationToken cancellationToken)
        {
            while (true)
            {
                var readTask = pipeline.ReadAsync(cancellationToken).AsTask();
                var completedTask = await Task.WhenAny(readTask, pipeline.Completion);
                if (completedTask == pipeline.Completion)
                    return;

                await ProcessQueuedFileAsync(readTask.Result);
            }
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

        private async ValueTask QueueImageFilesAsync(string path, ChannelWriter<Input> target)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                var input = new Input(file, ++_queuedFiles);

                if (!target.TryWrite(input))
                    await target.WriteAsync(input);

                _logger.LogInformation($"Queued {file} ({_queuedFiles})");
            }

            _logger.LogInformation($"Total # files queued: {_queuedFiles}");
        }
    }
}