using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AverageImage.Controllers
{
    [ApiController]
    public class ImageController : ControllerBase
    {
        private const int Preferred_Mode = 3;

        private readonly ILogger<ImageController> _logger;


        public ImageController(ILogger<ImageController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("api/image/get/url={url}/mode={mode}")]
        public async Task<string> Get(string url, int mode)
        {
            return await utils.Process(url, mode);
        }

        [HttpGet]
        [Route("api/image/get/url={url}")]
        public async Task<string> Get(string url)
        {
            return  await utils.Process(url, Preferred_Mode);
        }

        [HttpGet]
        [Route("api/image/")]
        public string Get()
        {
            return "Welcome";

            // As we have a logger injected here, we can use if for reporting purposes etc, for example
            // _logger.LogError(ex.ToString());

        }
    }
}
