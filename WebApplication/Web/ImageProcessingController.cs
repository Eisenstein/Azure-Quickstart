using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Domain;
using Microsoft.Azure.Cosmos;
using Azure.Messaging.ServiceBus;

namespace AzureQuickstart.Web
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageProcessingController(
        IConfiguration configuration,
        BlobServiceClient blobServiceClient, 
        CosmosClient cosmosClient,
        ServiceBusClient serviceBusClient) : ControllerBase
    {

        [HttpPut]
        public async Task<IActionResult> ProcessImage(IFormFile image)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(configuration["BlobSourceContainer"]);
            await containerClient.CreateIfNotExistsAsync();
            var fileId = Guid.NewGuid().ToString();
            await containerClient.UploadBlobAsync(fileId, image.OpenReadStream());

            var task = new Domain.Task(
                id: Guid.NewGuid().ToString(),
                fileName: image.FileName,
                originalPath: "/" + configuration["BlobSourceContainer"] + "/" + fileId,
                state: TaskState.Created
            );
            await cosmosClient
                .GetDatabase(configuration["Database"])
                .GetContainer(configuration["DbContainer"])
                .CreateItemAsync(task, new PartitionKey(task.id));

            await serviceBusClient
                .CreateSender(configuration["InitialTopicName"])
                .SendMessageAsync(new ServiceBusMessage(task.id));

            return Accepted((object)task.id);
        }

        [HttpGet("task/{taskId}/status")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTaskStatus(string taskId)
        {
            var task = await GetTask(taskId);
            if (task == null) 
            {
                return NotFound();
            }
            return Ok(task.state.ToString());
        } 

        [HttpGet("task/{taskId}/result")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status301MovedPermanently)]
        public async Task<IActionResult> GetTaskResult(string taskId)
        {
            var task = await GetTask(taskId);
            if (task == null) 
            {
                return NotFound();
            }
            if (task.state != TaskState.Done)
            {
                return BadRequest("Task is not done yet");
            }
            return RedirectPermanent(task.processedPath!);
        } 

        private async Task<Domain.Task?> GetTask(string taskId)
        {
            var taskResponse = await cosmosClient
                .GetDatabase(configuration["Database"])
                .GetContainer(configuration["DbContainer"])
                .ReadItemAsync<Domain.Task>(taskId, new PartitionKey(taskId));
            if (taskResponse.StatusCode != System.Net.HttpStatusCode.OK) 
            {
                return default;
            } 
            return taskResponse.Resource;
        }
    }
}
