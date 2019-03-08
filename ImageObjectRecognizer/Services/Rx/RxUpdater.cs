using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ImageMetadataUpdater.Models;
using ImageMetadataUpdater.Writers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageMetadataUpdater.Services.Rx
{
    internal class RxUpdater : IHostedService
    {
        private readonly ILogger<RxUpdater> _logger;
        private readonly IOptions<Configuration> _configuration;
        private readonly IResultWriter _resultWriter;
        private readonly  Subject<Input> _fileSubject = new Subject<Input>();
        private int _queuedFiles;

        public RxUpdater(ILogger<RxUpdater> logger, IOptions<Configuration> configuration, IResultWriter resultWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _resultWriter = resultWriter;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<Exception>();
            var recognizer = new Recognizer(_logger, _configuration);

            // Set up the consumer
            using (_fileSubject
                .AsObservable()
                .Select(input =>
                    Observable.Empty<Result>()
                        .Delay(TimeSpan.FromSeconds(1))
                        .Concat(Observable.FromAsync(() => recognizer.RecognizeAsync(input))))
                .Merge(10)
                .Subscribe(async result => { await _resultWriter.PersistResultAsync(result); }, e => tcs.TrySetResult(e),
                    () => tcs.TrySetResult(null)))
            {
                // Start the producer
                foreach (var file in Directory.EnumerateFiles(_configuration.Value.Path, "*.jpg",
                    SearchOption.TopDirectoryOnly))
                {
                    _fileSubject.OnNext(new Input(file, ++_queuedFiles));
                    _logger.LogInformation($"Queued {file}");
                }

                // Signal completion of producer
                _fileSubject.OnCompleted();

                // Wait for pipeline to drain
                var exception = await tcs.Task;
                if (exception != null)
                    throw exception;
            }
       }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
