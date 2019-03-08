# Image Object Recognizer

.Net Core Console Application that leverages the [Azure Cognitive Services Vision SDK](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/) to recognize objects in a jpg image file.

## Configuration

The configuration can be set using the commandline or the [appsettings.json](https://github.com/Expecho/ImageObjectRecognizer/blob/master/src/ImageObjectRecognizer/appsettings.json) file.

|Key|Description|
|---|---|
|SubscriptionKey|The key of the Azure Cognitive Services Resource. ([documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/vision-api-how-to-topics/howtosubscribe))| 
|Region|The region where the Azure Cognitive Services Resource is located.|
|Path|Full path to the local drive where the jpg files are located.|
|ConfidenceThreshold|The minimum [confidence score](https://docs.microsoft.com/nl-nl/dotnet/api/microsoft.azure.cognitiveservices.vision.computervision.models.detectedobject.confidence?view=azure-dotnet#Microsoft_Azure_CognitiveServices_Vision_ComputerVision_Models_DetectedObject_Confidence) for an object to be included in the result set.|
|Implementation|Name of the type that implements the service that will process the images. One of [those](https://github.com/Expecho/ImageObjectRecognizer/tree/master/src/ImageObjectRecognizer/Services).|

## Output

For each processed image file a json file with the same name as the image file is created with the result of the analysis. An example output looks like this:

```json
{
  "tags": [
    "mountain",
    "outdoor",
    "sky",
    "nature"
  ],
  "description": "a view of a large mountain in the background"
}
```
