using System.Text.Json;
using System.Text.Json.Serialization;

namespace XacAssist.JitM {
    public class PipelineConfig : IPipelineConfig {
        public string InputDevice { get; set; } = "/dev/input/js0";
        public string OutputDevice { get; set; } = "/dev/hidg0";

        private const string DEFAULT_SETTINGS_FILE = "controller_settings.json";
        private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions() {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private ILogger? _logger;
        private IConfiguration? _configuration;
        
        public PipelineConfig(ILogger<PipelineConfig> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
        }

        public PipelineConfig() {            
        }

        public void ReadConfiguration() {
            string configFile = _configuration.GetValue<string>("Options:ControllerSettingsFile", DEFAULT_SETTINGS_FILE);
            if (File.Exists(configFile)) {
                FromJSON(File.ReadAllText(configFile));
            }            
        }

        public void FromJSON(string jsonString) {
            PipelineConfig? newConfig = JsonSerializer.Deserialize<PipelineConfig>(jsonString, _jsonOptions);

            if (newConfig != null) {
                newConfig._logger = _logger;
                newConfig._configuration = _configuration;
                InputDevice = newConfig.InputDevice;
                OutputDevice = newConfig.OutputDevice;
            }
        }        

        public void Save() {
            string configFile = _configuration.GetValue<string>("Options:ControllerSettingsFile", DEFAULT_SETTINGS_FILE);
            File.WriteAllText(configFile, JsonSerializer.Serialize<PipelineConfig>(this, _jsonOptions));
        }
    }
}