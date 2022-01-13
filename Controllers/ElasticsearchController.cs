using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ElasticsearchRestaurantsApiNetcore.Helpers;
using System.Net.Mime;
using System.Linq;
using System.IO;

namespace ElasticsearchRestaurantsApiNetcore.Controllers
{
    [ApiController]
    [Route("/")]
    public class ElasticsearchController : ControllerBase
    {
        private readonly ILogger<ElasticsearchController> _logger;
        readonly Helper evalHelper = null;
        ElasticClient client = null;

        public ElasticsearchController(ILogger<ElasticsearchController> logger)
        {
            _logger = logger;
            client = Client.ElasticClient;
            evalHelper = new Helper();
        }

        [HttpPost("search")]
        public IActionResult search()
        {
            string query = string.Empty;
            try
            {
                if (HttpContext.Request.HasFormContentType)
                    query = string.Join("&", Request.Form.Select(i => $"{i.Key}={i.Value}"));
                else
                    query = new StreamReader(HttpContext.Request.Body).ReadToEndAsync().Result;
            }
            catch (System.Exception)
            {
            }

            JObject result = SearchHelper.ConvertToParams(query);
            if (result == null || result.HasValues == false)
                return new JsonResult(new { error = "Not a valid query." });

            result = SearchHelper.GetGeoSearchParams(result);
            string index = result.Value<string>("index");
            if (string.IsNullOrWhiteSpace(index))
                index = Client.IndexName;

            bool sleep = result.Value<bool>("sleep");
            //for test
            if (sleep)
                System.Threading.Thread.Sleep(5000);

            // demo only 80 results
            var searchResponse = client.LowLevel.Search<StringResponse>(index, PostData.String(JsonConvert.SerializeObject(new
            {
                query = result.Value<JToken>("query"),
                size = 80
            })));

            _logger.LogInformation(System.Text.Encoding.UTF8.GetString(searchResponse.RequestBodyInBytes));
            _logger.LogInformation(searchResponse.Body);
            return Content(System.Text.Encoding.UTF8.GetString(searchResponse.ResponseBodyInBytes), MediaTypeNames.Application.Json);
        }

        [HttpGet("doc/{id}")]
        public IActionResult GetDoc(string id, string index = "")
        {
            var result = evalHelper.GetDoc(id, index);
            return Content(System.Text.Encoding.UTF8.GetString(result.ApiCall.ResponseBodyInBytes), MediaTypeNames.Application.Json);
        }
    }
}
