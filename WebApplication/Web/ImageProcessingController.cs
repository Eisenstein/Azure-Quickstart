using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;

namespace AzureQuickstart.Web
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageProcessingController(BlobServiceClient blobServiceClient) : ControllerBase
    {
        private static readonly string ContainerName = "images";

        [HttpPut]
        public async Task<string> ProcessImage(IFormFile image)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();
            var taskId = Guid.NewGuid().ToString();
            await containerClient.UploadBlobAsync(taskId, image.OpenReadStream());
            return taskId;
        }
    }
}
