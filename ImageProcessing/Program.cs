using Azure.Messaging.ServiceBus;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;
using System.Drawing;

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions{
            ExcludeInteractiveBrowserCredential = false
        });

var cosmosClient = new CosmosClient("https://image-processing.documents.azure.com:443/", credential);
var blobServiceClient = new BlobServiceClient(new Uri("https://aisekquickstart.blob.core.windows.net"), credential);

async Task MessageHandler(ProcessMessageEventArgs args)
{
    string taskId = args.Message.Body.ToString();
    var task = await cosmosClient
        .GetDatabase("tasks")
        .GetContainer("tasks")
        .ReadItemAsync<Domain.Task>(taskId, new PartitionKey(taskId));

    var fileContentStream = await new BlobClient(new Uri("https://aisekquickstart.blob.core.windows.net" + task.Resource.originalPath), credential)
        .OpenReadAsync();
    var image = Image.FromStream(fileContentStream);
    var stream = new MemoryStream();
    image.RotateFlip(RotateFlipType.RotateNoneFlipY);
    image.Save(stream, image.RawFormat);
    stream.Position = 0;
    
    var fileId = Guid.NewGuid().ToString();
    await blobServiceClient
        .GetBlobContainerClient("processed-images")
        .UploadBlobAsync(fileId, stream);

    await cosmosClient
        .GetDatabase("tasks")
        .GetContainer("tasks")
        .ReplaceItemAsync(task.Resource with { processedPath = "/" + "processed-images" + "/" + fileId }, task.Resource.id, new PartitionKey(task.Resource.id));

    await args.CompleteMessageAsync(args.Message);
}

Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}

var client = new ServiceBusClient("image-proc.servicebus.windows.net", credential);

var processor = client.CreateProcessor("new-messages", "s1", new ServiceBusProcessorOptions());

try
{
    processor.ProcessMessageAsync += MessageHandler;
    processor.ProcessErrorAsync += ErrorHandler;
    await processor.StartProcessingAsync();

}
finally
{
    await processor.DisposeAsync();
    await client.DisposeAsync();
}