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
        private readonly ILogger<ImageController> _logger;
        private ServiceImageUtils _ServiceImageUtils { get; set; }

        public ImageController(ILogger<ImageController> logger, ServiceImageUtils serviceImageUtils)
        {
            _logger = logger;
            _ServiceImageUtils = serviceImageUtils;
        }

        [HttpGet]
        [Route("api/image/getaveragecolour/imageurl={imageurl}")]
        public async Task<string> GetAverageColour(string imageUrl)
        {
            return  await _ServiceImageUtils.Process(imageUrl);
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
