using Microsoft.AspNetCore.Mvc;

using System.Text.Json;
using System.Text.Json.Serialization;

using XacAssist.JitM;
using XacAssist.Extensions;

namespace XacAssist.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class ConfigController : ControllerBase
    {    
        private readonly ILogger<ConfigController> _logger;
        private readonly IPipelineConfig _configuration;
        private readonly IPipeline _pipeline;

        public ConfigController(ILogger<ConfigController> logger, IPipelineConfig configuration, IPipeline pipeline) {
            _logger = logger;
            _configuration = configuration;
            _pipeline = pipeline;
        }

        [HttpGet]
        public IActionResult Get() {                        
            return Ok(_configuration);
        }

        [HttpPost]
        public async Task<IActionResult> Set() {               
            string rawBody = await Request.GetRawBodyAsync();        
            return ReinitializeWithSettings(rawBody, false);
        }

        [HttpPut]
        public async Task<IActionResult> SetAndSave() {       
            string rawBody = await Request.GetRawBodyAsync();
            return ReinitializeWithSettings(rawBody, true);
        }

        private IActionResult ReinitializeWithSettings(string rawSettings, bool save) {
            _logger.LogDebug($"Provided new config - Save to File => {save} Configuration: {rawSettings}");            
            
            _configuration.FromJSON(rawSettings);

            if(save) { 
                _configuration.Save();
            }
            return Ok(_configuration);
        }
    }
}