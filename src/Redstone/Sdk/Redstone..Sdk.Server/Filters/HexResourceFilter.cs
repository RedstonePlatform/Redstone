using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Redstone.Sdk.Server.Services;

namespace Redstone.Sdk.Server.Filters
{
    public class HexResourceFilter : IAsyncResourceFilter
    {
        private readonly ITokenService _tokenService;

        public HexResourceFilter(ITokenService tokenService)
        {
            this._tokenService = tokenService;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            try
            {
                // TODO: key needs to be configurable - perhaps through a service
                var token = await this._tokenService.AcceptHex();
                context.Result = new OkObjectResult(new { token });
            }
            catch (Exception)
            {
                context.Result = new BadRequestObjectResult("Redstone Hex wasn't accepted by the node");
            }
        }
    }
}
