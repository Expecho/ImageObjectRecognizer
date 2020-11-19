# Image Object Recognizer

.Net Core Console Application that leverages the [Azure Cognitive Services Vision SDK](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/) to recognize objects in a jpg image file.

## How it works

The application will loop recursively through all directories in the path specified in the configuration. It will then send them individually to the API to have them analyzed. The result is stored as json in a file created with the same name as the image in the same folder. 

The main purpose of this repository is not the tool itself, but the demonstration of several ways to call the [rate limited api](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/computer-vision/) using multithreading techniques. When calls have to be throttled, backpressure has to be applied to the producer in a producer/consumer scenario. There are several implementations of the producer/consumer scenario in this repository:

The current implemetations are based on the S1 pricing tier that allows for 10 calls per second to the computer vision api.

|Technique|Description|
|---|---|
|[BlockingCollection](https://github.com/Expecho/ImageObjectRecognizer/blob/master/src/ImageObjectRecognizer/Services/BlockingCollectionService.cs)|[Blocking Collections](https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview) supports limiting the concurrency by creating a bounded instance.|
|[TPL DataFlow](https://github.com/Expecho/ImageObjectRecognizer/blob/master/src/ImageObjectRecognizer/Services/DataFlowService.cs)|[TPL Dataflow](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library) is a good fit for this task since it allows you to create a pipeline for the process and apply concurrencly limits per step in the pipeline.|
|[PLinq](https://github.com/Expecho/ImageObjectRecognizer/blob/master/src/ImageObjectRecognizer/Services/PlinqService.cs)|Though [Parallel Linq](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/parallel-linq-plinq) supports limiting the concurrency using `WithDegreeOfParallelism`, it does not work well with the async-await pattern since it does not support `Task` or `Task<T>`.|
|[Tasks](https://github.com/Expecho/ImageObjectRecognizer/blob/master/src/ImageObjectRecognizer/Services/TaskBasedService.cs)|Using ` Task` or `Task<T>` does not support limiting concurrency.|
|[Channels](https://github.com/Expecho/ImageObjectRecognizer/blob/master/src/ImageObjectRecognizer/Services/ChannelsService.cs)|[Channels](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels?view=dotnet-plat-ext-2.1) are somewhat similar to blocking collections but have some advantages. They support limiting the concurrency by creating a bounded instance.|
|[Reactive Extensions](https://github.com/Expecho/ImageObjectRecognizer/blob/master/src/ImageObjectRecognizer/Services/ReactiveExtensionsService.cs)|[Reactive Extensions](https://github.com/dotnet/reactive) is the best fit for this particular task since it can throttle based on time.|

## Cognitive Services Computer Vision API

Use the template below to provision an Azure Cognitive Services Resource. This is a required step for the application to function. An already existing resource can be used as well.

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
|ImagesPath|Full path to the local drive where the jpg files are located.|Empty|
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
