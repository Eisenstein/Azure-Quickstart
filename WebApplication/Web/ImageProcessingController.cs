using Microsoft.AspNetCore.Mvc;

namespace AzureQuickstart.Web
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageProcessingController : ControllerBase
    {
        [HttpGet]
        public string HelloWorld()
        {
            return "Hello, World!";
        }
    }
}
