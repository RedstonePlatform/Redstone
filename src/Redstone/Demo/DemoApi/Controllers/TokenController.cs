using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Redstone.Sdk.Server.Services;

namespace DemoApi.Controllers
{
    [Route("v1/[controller]")]
    //[Authorize(AuthenticationSchemes = RedstoneAuthenticationSchemes.Hex)]
    public class TokenController : Controller
    {
        private readonly ITokenService _tokenService;

        public TokenController(ITokenService tokenService)
        {
            this._tokenService = tokenService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var token = await this._tokenService.AcceptHex(1, "redstoneredstone");
                return Ok(new { token });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }
    }
}
