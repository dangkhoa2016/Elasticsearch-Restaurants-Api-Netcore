using System.Net.Mime;
using DynamicExpresso;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ElasticsearchRestaurantsApiNetcore.Controllers
{
    [Route("/")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        readonly Helpers.Helper helper = null;
        //static ElasticLowLevelClient jsonClient = new ElasticLowLevelClient(new ConnectionConfiguration(node));
        static Interpreter interpreter = new Interpreter().EnableReflection()
            .SetVariable("client", Helpers.Client.ElasticClient)
            .SetVariable("helper", new Helpers.Helper())
            .SetVariable("jsonClient", Helpers.Client.ElasticClient.LowLevel);
        //.SetVariable("jsonClient", jsonClient);

        readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger, IHostingEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            helper = new Helpers.Helper(null, logger);
        }

        [HttpGet("/")]
        public IActionResult Welcome()
        {
            return new JsonResult(new { hello = "world" });
        }


        static bool IsPropertyExist(dynamic obj, string name)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(name);

            return obj.GetType().GetProperty(name) != null;
        }

        [HttpPost("import")]
        public async Task<IActionResult> RunBulkIndex()
        {
            try
            {
                await helper.InitIndex();
                int total = 0;

                string filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Config", "au-final.json");
                if (System.IO.File.Exists(filePath))
                {
                    var restaurants = JArray.Parse(System.IO.File.ReadAllText(filePath));
                    int bulkSize = 30;
                    bool success = false;
                    Dictionary<string, string> lst = null;

                    Func<JToken, string, bool> removeKey = (token, key) =>
                    {
                        if (token != null)
                        {
                            var field = token[key];
                            if (field != null)
                                field.Parent.Remove();
                        }

                        return true;
                    };

                    List<string> lstToRemove = new List<string>() { "restaurantId", "_id", "diningStyle" };
                    for (int i = 0, j = restaurants.Count; i < j; i += bulkSize)
                    {
                        lst = new Dictionary<string, string>();

                        for (var n = i; n < i + bulkSize; n++)
                        {
                            if (n < j)
                            {
                                string id = restaurants[n].Value<string>("restaurantId");

                                foreach (var item in lstToRemove)
                                    removeKey(restaurants[n], item);

                                lst.Add(id, JsonConvert.SerializeObject(restaurants[n]));
                            }
                            else
                                break;
                        }

                        if (lst != null && lst.Count > 0)
                        {
                          success=  await helper.BulkIndexDocument(lst); 
                            if (success)
                            {
                                total += lst.Count;
                                _logger.LogInformation($"Done: {lst.Count}, Total: {total}");
                            }
                        }
                    }
                }

                return new JsonResult(new { msg = $"All done: {total}" });
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }


        [HttpPost("eval")]
        public IActionResult Eval([FromForm] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new JsonResult(new { error = "Please provide code." });

            // client.RootNodeInfo();
            // client.Cat.Nodes();
            //client.Indices.Get(Indices.Index("restaurants"));

            Func<dynamic, ContentResult> response = (value) =>
            {
                var responseData = Convert.ToString(value);
                _logger.LogInformation((string)responseData);
                return Content(responseData, MediaTypeNames.Text.Plain);
            };

            dynamic result = interpreter.Parse(content).Invoke();
            if (IsPropertyExist(result, "ApiCall"))
            {
                var z = result.ApiCall;

                _logger.LogInformation((string)result.DebugInformation);
                return Content(System.Text.Encoding.UTF8.GetString(z.ResponseBodyInBytes), MediaTypeNames.Application.Json);
            }
            else if (IsPropertyExist(result, "Result"))
                return response(result.Result);

            return response(result);
        }
    }
}