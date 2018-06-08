using Microsoft.AspNetCore.Mvc;
using Redstone.Sdk.Server.Filters;

namespace DemoApi.Controllers
{
    [Route("v1/[controller]")]
    public class TokenController : Controller
    {
        [HttpGet]
        [ServiceFilter(typeof(HexResourceFilter))]
        public void Get()
        {
        }
    }
}
