using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageObjectRecognizer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ImageObjectRecognizer.Writers
{
    public class FileWriter : IResultWriter
    {
        private readonly ILogger<FileWriter> _logger;
        private readonly IOptions<Configuration> _configuration;

        public FileWriter(ILogger<FileWriter> logger, IOptions<Configuration> configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task PersistResultAsync(Result result)
        {
            _logger.LogInformation($"Writing {result.Input.FilePath} ({result.Input.FileIndex}).");

            var tags = result.ImageAnalysis.Tags.Where(tag => tag.Confidence > _configuration.Value.ConfidenceThreshold).Select(tag => tag.Name);
            var description = result.ImageAnalysis.Description.Captions.OrderByDescending(caption => caption.Confidence)
                .FirstOrDefault(caption => caption.Confidence > _configuration.Value.ConfidenceThreshold)?.Text;

            var json = JsonConvert.SerializeObject(new { tags, description }, Formatting.Indented);
            await File.WriteAllTextAsync(Path.Combine(
                    Path.GetDirectoryName(result.Input.FilePath),
                    Path.GetFileNameWithoutExtension(result.Input.FilePath) + ".json")
                , json);
        }
    }
}