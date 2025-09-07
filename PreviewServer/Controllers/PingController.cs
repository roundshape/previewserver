using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace PreviewServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("pong");
        }
    }
}
