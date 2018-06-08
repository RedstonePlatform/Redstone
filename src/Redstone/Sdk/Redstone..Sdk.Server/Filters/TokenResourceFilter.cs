using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Redstone.Sdk.Server.Services;

namespace Redstone.Sdk.Server.Filters
{
    public class TokenResourceFilter : IAsyncResourceFilter
    {
        private readonly ITokenService _tokenService;

        public TokenResourceFilter(ITokenService tokenService)
        {
            this._tokenService = tokenService;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (!await this._tokenService.ValidateTokenAsync("redstoneredstone").ConfigureAwait(false))
                context.Result = new BadRequestObjectResult("Redstone token not valid");
            else
                await next();
        }
    }
}
