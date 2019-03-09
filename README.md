# Image Object Recognizer

.Net Core Console Application that leverages the [Azure Cognitive Services Vision SDK](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/) to recognize objects in a jpg image file.

## Cognitive Services Computer Vision API

Use the template below to provision an Azure Cognitive Services Resource.

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-quickstart-templates%2Fmaster%2F101-cognitive-services-Computer-vision-API%2Fazuredeploy.json" target="_blank">
<img src="https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-quickstart-templates%2Fmaster%2F101-cognitive-services-Computer-vision-API%2Fazuredeploy.json" target="_blank">
<img src="https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/visualizebutton.png"/>
</a>

This template deploys an Cognitive Services Computer Vision API.
In the outputs section it will show the Keys and the Endpoint.

## Configuration

The configuration can be set using the commandline or the [appsettings.json](https://github.com/Expecho/ImageObjectRecognizer/blob/master/src/ImageObjectRecognizer/appsettings.json) file.

|Key|Description|Default|
|---|---|---|
|SubscriptionKey|The key of the Azure Cognitive Services Resource. ([documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/vision-api-how-to-topics/howtosubscribe))|Empty|
|Region|The region where the Azure Cognitive Services Resource is located.|Empty|
|Path|Full path to the local drive where the jpg files are located.|Empty|
|ConfidenceThreshold|The minimum [confidence score](https://docs.microsoft.com/nl-nl/dotnet/api/microsoft.azure.cognitiveservices.vision.computervision.models.detectedobject.confidence?view=azure-dotnet#Microsoft_Azure_CognitiveServices_Vision_ComputerVision_Models_DetectedObject_Confidence) for an object to be included in the result set.|0.75|
|Implementation|Name of the type that implements the service that will process the images. One of [those](https://github.com/Expecho/ImageObjectRecognizer/tree/master/src/ImageObjectRecognizer/Services).|ReactiveExtensionsService|

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
