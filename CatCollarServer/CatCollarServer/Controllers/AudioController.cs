using CatCollarServer.Command;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CatCollarServer.Controllers
{
    [EnableCors("myAllowSpecificOrigins")]
    [ApiController]
    [Route("[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly ILogger<AudioController> _logger;

        public AudioController(ILogger<AudioController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/devices")]
        public async Task<ActionResult<IEnumerable<Devices>>> Get()
        {
            return CommandFacad.Devices.Select(x => new Devices() { Name = x }).ToList();
        }

        [HttpGet("{deviceName}")]
        public async Task<ActionResult<AudioResult>> Get(string deviceName)
        {

            if (!CommandFacad.Devices.Contains(deviceName))
            {
                return new AudioResult()
                {
                    Result = "Device disconnected",
                    Color = "Gray"
                };
            }
            CommandFacad.Recognize(deviceName);
            if (CommandFacad.Result == null)
            {
                return new AudioResult()
                {
                    Result = "Server error",
                    Color = "Gray"
                };
            }
            return new AudioResult()
            {
                Result = CommandFacad.Result,
                Color = CommandFacad.EmotionColorPair.ContainsKey(CommandFacad.Result) ? CommandFacad.EmotionColorPair[CommandFacad.Result] : "Gray"
            };
        }
    }
}
