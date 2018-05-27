using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Redstone.Sdk.Server.Services;

namespace DemoApi.Controllers
{
    [Route("v1/[controller]")]
    //[Authorize(AuthenticationSchemes = RedstoneAuthenticationSchemes.Token)]
    public class DemoController : Controller
    {
        private readonly ITokenService _tokenService;

        public DemoController(ITokenService tokenService)
        {
            this._tokenService = tokenService;
        }

        // GET api/values
        [HttpGet]
        public IActionResult Get()
        {
            if (!this._tokenService.ValidateToken("redstoneredstone"))
                return BadRequest(new { error = "redstone token not valid" });

            return Ok(new string[] { "value1", "value2" });
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            if (!this._tokenService.ValidateToken("redstoneredstone"))
                return BadRequest(new { error = "redstone token not valid" });

            return Ok("value");
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
