using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ElasticsearchRestaurantsApiNetcore.Controllers
{
    [Route("/")]
    [ApiController]
    public class OtherRouteController : ControllerBase
    {
        IWebHostEnvironment _env = null;
        public OtherRouteController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
        }

        [HttpGet]
        [Route("favicon.png")]
        public IActionResult Favicon_png()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "imgs/klc_favicon.png");
            return PhysicalFile(filePath, "image/png");
        }

        [HttpGet]
        [Route("favicon.ico")]
        public IActionResult Favicon_ico()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "imgs/klc_favicon.ico");
            return PhysicalFile(filePath, "image/x-icon");
        }
    }
}