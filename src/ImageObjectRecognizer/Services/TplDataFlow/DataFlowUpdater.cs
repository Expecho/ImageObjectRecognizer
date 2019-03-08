using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ImageMetadataUpdater.Models;
using ImageMetadataUpdater.Writers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageMetadataUpdater.Services.TplDataFlow
{
    internal class DataFlowUpdater : IHostedService
    {
        private readonly ILogger<DataFlowUpdater> _logger;
        private readonly IOptions<Configuration> _configuration;
        private readonly IResultWriter _resultWriter;
        private int _queuedFiles;

        public DataFlowUpdater(ILogger<DataFlowUpdater> logger, IOptions<Configuration> configuration, IResultWriter resultWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _resultWriter = resultWriter;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var recognizer = new Recognizer(_logger, _configuration);

            // Set up the consumer (DataFlow pipeline)
            var transformer = new TransformBlock<Input, Result>(async input =>
            {
                _logger.LogInformation($"Transforming {input.FilePath} ({input.FileIndex}).");
                return await recognizer.RecognizeAsync(input);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 10
            });

            var writer = new ActionBlock<Result>(async result =>
            {
                await _resultWriter.PersistResultAsync(result);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            });

            transformer.LinkTo(writer, new DataflowLinkOptions { PropagateCompletion = true });

            // Start the producer
            QueueImageFiles(_configuration.Value.Path, transformer);

            // Signal completion
            transformer.Complete();

            // Wait for pipeline to drain
            await writer.Completion;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void QueueImageFiles(string path, ITargetBlock<Input> target)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                target.Post(new Input(file, ++_queuedFiles));
                _logger.LogInformation($"Queued {file} ({_queuedFiles})");
            }
        }
    }
}