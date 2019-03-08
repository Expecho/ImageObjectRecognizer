using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace ImageMetadataUpdater.Models
{
    public class Result
    {
        public Result(Input input, ImageAnalysis imageAnalysis)
        {
            Input = input;
            ImageAnalysis = imageAnalysis;
        }

        public Input Input { get; private set; }
        public ImageAnalysis ImageAnalysis { get; private set; }
    }
}