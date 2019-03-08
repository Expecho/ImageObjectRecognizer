namespace ImageMetadataUpdater
{
    public class Configuration
    {
        public string SubscriptionKey { get; set; }
        public string Region { get; set; }
        public string Path { get; set; }
        public double ConfidenceThreshold { get; set; }
        public string Implementation { get; set; }
    }
}