using System;
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
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(5);
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

            _logger.LogInformation($"Total # files queued: {_queuedFiles}");

            // Wait for process to complete
            await Task.WhenAll(tasks);

            Console.WriteLine("Finished. Press any key to exit.");
            Console.ReadKey();

            // Set up the consumer
            async Task CreateWorker(Input input)
            {
                await _semaphoreSlim.WaitAsync();

                _logger.LogInformation($"Transforming {input.FilePath} ({input.FileIndex}).");

                try
                {
                    var result = await recognizer.RecognizeAsync(input);
                    await _resultWriter.PersistResultAsync(result);
                    _logger.LogInformation($"Transformed {result.Input.FilePath} ({result.Input.FileIndex}).");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error processing {input.FilePath} ({input.FileIndex}): {e.Message}");
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
