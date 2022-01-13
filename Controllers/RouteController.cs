using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace ElasticsearchRestaurantsApiNetcore.Controllers
{
    [Route("routes")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

        public RouteController(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        public RootResultModel Get()
        {
            var routes = _actionDescriptorCollectionProvider.ActionDescriptors.Items.Where(
                ad => ad.AttributeRouteInfo != null).Select(ad => new RouteModel
                {
                    Name = ad.AttributeRouteInfo.Template,
                    Order = ad.AttributeRouteInfo.Order,
                    Method = ad.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods.First(),
                }).ToList();

            var res = new RootResultModel
            {
                Routes = routes
            };

            return res;
        }
    }

    public class RouteModel
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public string Template { get; set; }
        public string Method { get; set; }
    }

    public class RootResultModel
    {
        public List<RouteModel> Routes { get; set; }
    }
}