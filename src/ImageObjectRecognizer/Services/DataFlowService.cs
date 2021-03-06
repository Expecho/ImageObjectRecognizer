﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ImageObjectRecognizer.Models;
using ImageObjectRecognizer.Writers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageObjectRecognizer.Services
{
    internal class DataFlowService : IHostedService
    {
        private readonly ILogger<DataFlowService> _logger;
        private readonly IOptions<Configuration> _configuration;
        private readonly IResultWriter _resultWriter;
        private int _queuedFiles;

        public DataFlowService(ILogger<DataFlowService> logger, IOptions<Configuration> configuration, IResultWriter resultWriter)
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
                _logger.LogInformation($"Transformed {result.Input.FilePath} ({result.Input.FileIndex}).");
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            });

            transformer.LinkTo(writer, new DataflowLinkOptions { PropagateCompletion = true });

            // Start the producer
            QueueImageFiles(_configuration.Value.ImagesPath, transformer);

            // Signal completion
            transformer.Complete();

            // Wait for pipeline to drain
            await writer.Completion;

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();
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

            _logger.LogInformation($"Total # files queued: {_queuedFiles}");
        }
    }
}