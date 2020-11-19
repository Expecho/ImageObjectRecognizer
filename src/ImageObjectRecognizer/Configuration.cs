namespace ImageObjectRecognizer
{
    public class Configuration
    {
        public string SubscriptionKey { get; set; }
        public string Region { get; set; }
        public string ImagesPath { get; set; }
        public double ConfidenceThreshold { get; set; }
        public string Implementation { get; set; }
    }
}