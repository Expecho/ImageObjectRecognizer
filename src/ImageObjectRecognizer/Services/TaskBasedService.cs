﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageObjectRecognizer.Models;
using ImageObjectRecognizer.Writers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageObjectRecognizer.Services
{
    internal class TaskBasedService : IHostedService
    {
        private readonly ILogger<TaskBasedService> _logger;
        private readonly IOptions<Configuration> _configuration;
        private readonly IResultWriter _resultWriter;
        private int _queuedFiles;

        public TaskBasedService(ILogger<TaskBasedService> logger, IOptions<Configuration> configuration, IResultWriter resultWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _resultWriter = resultWriter;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var recognizer = new Recognizer(_logger, _configuration);
            var tasks = new List<Task>();

            // Start the producer
            foreach (var file in Directory.EnumerateFiles(_configuration.Value.Path, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                tasks.Add(CreateWorker(new Input(file, ++_queuedFiles)));
                _logger.LogInformation($"Queued {file} ({_queuedFiles})");
            }

            // Wait for process to complete
            await Task.WhenAll(tasks);
            
            // Set up the consumer
            async Task CreateWorker(Input input)
            {
                var result = await recognizer.RecognizeAsync(input);
                await _resultWriter.PersistResultAsync(result);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
