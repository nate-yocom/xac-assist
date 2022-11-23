using System.Text.Json;
using System.Text.Json.Serialization;

namespace XacAssist.JitM {
    public class PipelineConfig : IPipelineConfig {
        public string InputDevice { get; set; } = "/dev/input/js0";
        public string OutputDevice { get; set; } = "/dev/hidg0";
        public HashSet<byte> FireAndResetAxes { get; set; } = new HashSet<byte>() { 0x00, 0x01 };        
        public int WaitToReset { get; set; } = 50;
        public float FireThreshold { get; set; } = 0.80f;
        public float ResetThreshold { get; set; } = 0.15f;
        public bool IgnoreAllButtons { get; set; } = true;
        public bool IgnoreAllAxes { get; set; } = false;
        public bool AllowAxisHoldToFlow { get; set; } = true;
        public int AxisHoldToFlowHoldTimeMilliseconds { get; set; } = 1500;


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
                _logger?.LogDebug($"Pre-update ======> ");
                LogConfiguration();
                _logger?.LogDebug($"Incoming config ======> ");
                newConfig.LogConfiguration();                
                InputDevice = newConfig.InputDevice;
                OutputDevice = newConfig.OutputDevice;
                FireAndResetAxes = newConfig.FireAndResetAxes;
                WaitToReset = newConfig.WaitToReset;
                FireThreshold = newConfig.FireThreshold;
                ResetThreshold = newConfig.ResetThreshold;
                IgnoreAllButtons = newConfig.IgnoreAllButtons;
                IgnoreAllAxes = newConfig.IgnoreAllAxes;
                AllowAxisHoldToFlow = newConfig.AllowAxisHoldToFlow;
                AxisHoldToFlowHoldTimeMilliseconds = newConfig.AxisHoldToFlowHoldTimeMilliseconds;
                _logger?.LogDebug($"Post-update ======> ");
                LogConfiguration();
            }
        }        

        public void Save() {
            string configFile = _configuration.GetValue<string>("Options:ControllerSettingsFile", DEFAULT_SETTINGS_FILE);
            File.WriteAllText(configFile, JsonSerializer.Serialize<PipelineConfig>(this, _jsonOptions));
        }

        private void LogConfiguration() {
            _logger?.LogInformation($"InputDevice: {InputDevice} OutputDevice: {OutputDevice}");
            string fireAxes = String.Join(",", FireAndResetAxes.Select(x => x.ToString()));
            _logger?.LogInformation($"FireAndResetAxes: {fireAxes} WaitToReset: {WaitToReset} FireThreshold: {FireThreshold} ResetThreshold: {ResetThreshold}");
            _logger?.LogInformation($"IgnoreAllButtons: {IgnoreAllButtons} IgnoreAllAxes: {IgnoreAllAxes}");
            _logger?.LogInformation($"AllowHoldToFlow: {AllowAxisHoldToFlow} Time: {AxisHoldToFlowHoldTimeMilliseconds}");
        }
    }
}