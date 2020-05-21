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
            var u = new utils();
            return await u.Process(url, mode);
        }

        [HttpGet]
        [Route("api/image/get/url={url}")]
        public async Task<string> Get(string url)
        {
            var u = new utils();
            return  await u.Process(url, Preferred_Mode);
        }

        [HttpGet]
        [Route("api/image/")]
        public string Get()
        {
            return "Welcome";
        }
    }
}
