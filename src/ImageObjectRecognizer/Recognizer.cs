using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageObjectRecognizer.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageObjectRecognizer
{
    public class Recognizer
    {
        private readonly ILogger _logger;
        private readonly ComputerVisionClient _computerVision;

        private static readonly List<VisualFeatureTypes> Features = new List<VisualFeatureTypes>
        {
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Tags
        };

        public Recognizer(ILogger logger, IOptions<Configuration> configuration)
        {
            _logger = logger;
            _computerVision = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(configuration.Value.SubscriptionKey))
            {
                Endpoint = $"https://{configuration.Value.Region}.api.cognitive.microsoft.com"
            };
        }

        public async Task<Result> RecognizeAsync(Input input)
        {
            using (var imageStream = File.OpenRead(input.FilePath))
            {
                _logger.LogInformation($"Sending for analysis: {input.FilePath}");

                return new Result(
                        input,
                        await _computerVision.AnalyzeImageInStreamAsync(imageStream, Features));
            }
        }
    }
}