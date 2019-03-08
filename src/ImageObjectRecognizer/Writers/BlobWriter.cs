using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ImageMetadataUpdater.Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;

namespace ImageMetadataUpdater.Writers
{
    public class BlobWriter : IResultWriter
    {
        private readonly ILogger<FileWriter> _logger;
        private readonly IOptions<Configuration> _configuration;

        public BlobWriter(ILogger<FileWriter> logger, IOptions<Configuration> configuration)
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

            var storageAccount = CloudStorageAccount.Parse("");
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("");
            var blob = container.GetBlockBlobReference(result.Input.FilePath);
            blob.Metadata["tags"] = string.Join(",", tags);
            blob.Metadata["description"] = description;

            await blob.SetMetadataAsync();
        }
    }
}
