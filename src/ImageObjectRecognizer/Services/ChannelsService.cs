using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMetadataUpdater.Models;
using ImageMetadataUpdater.Writers;
using System.Threading.Channels;

namespace ImageMetadataUpdater.Services
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
            var pipeline = Channel.CreateUnbounded<Input>();

            // Start the consumer
            var processingTask = ProcessQueuedFilesAsync(pipeline.Reader, cancellationToken);

            // Start producing data
            await QueueImageFilesAsync(_configuration.Value.Path, pipeline.Writer);

            // Signal producer completion
            pipeline.Writer.Complete();

            // Wait for pipeline to drain
            await processingTask;
        }

        private Task ProcessQueuedFilesAsync(ChannelReader<Input> pipeline, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while(true)
                {
                    var readTask = pipeline.ReadAsync(cancellationToken).AsTask();
                    var completedTask = await Task.WhenAny(readTask, pipeline.Completion);
                    if (completedTask == pipeline.Completion)
                        return;

                    await ProcessQueuedFileAsync(readTask.Result);
                }
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

        private async Task QueueImageFilesAsync(string path, ChannelWriter<Input> target)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                await target.WriteAsync(new Input(file, ++_queuedFiles));
                _logger.LogInformation($"Queued {file} ({_queuedFiles})");
            }
        }
    }
}