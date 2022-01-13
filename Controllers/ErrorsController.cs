using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ElasticsearchRestaurantsApiNetcore.Controllers
{
    [Route("/")]
    [ApiController]
    public class ErrorsController : ControllerBase
    {
        //private readonly ILogger<ErrorsController> _logger;
        public ErrorsController(ILogger<ErrorsController> logger)
        {
            //_logger = logger;
        }

        [HttpGet("404")]
        public IActionResult ShowNotFound()
        {
            HttpContext.Response.StatusCode = 404;
            return new JsonResult(new { error = "404 Route not found.", msg = "Please go home" });
        }

        [HttpGet("500")]
        public IActionResult ShowException()
        {
            HttpContext.Response.StatusCode = 500;
            return new JsonResult(new { error = "500 Internal Server Error.", msg = "Please go home" });
        }
    }
}