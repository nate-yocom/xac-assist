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
        public HashSet<byte> IgnoredButtons { get; set; } = new HashSet<byte>();
        public HashSet<byte> IgnoredAxes { get; set; } = new HashSet<byte>();                
        public Dictionary<byte, byte> MappedButtons { get; set; } = new Dictionary<byte, byte>();
        public Dictionary<byte, byte> MappedAxes { get; set; } = new Dictionary<byte, byte>();


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

        public byte MapButtonIfMapped(byte inputButton) {
            return MappedButtons.ContainsKey(inputButton) ? MappedButtons[inputButton] : inputButton;
        }

        public byte MapAxisIfMapped(byte inputAxis) {
            return MappedAxes.ContainsKey(inputAxis) ? MappedAxes[inputAxis] : inputAxis;
        }

        public bool IsIgnoreButton(byte inputButton) {
            return IgnoreAllButtons || IgnoredButtons.Contains(inputButton);
        }

        public bool IsIgnoreAxis(byte inputAxis) {
            return IgnoreAllAxes || IgnoredAxes.Contains(inputAxis);
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
                IgnoredButtons = newConfig.IgnoredButtons;
                IgnoredAxes = newConfig.IgnoredAxes;
                MappedButtons = newConfig.MappedButtons;
                MappedAxes = newConfig.MappedAxes;
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
            string ignoredButtonsList = String.Join(",", IgnoredButtons.Select(x => x.ToString()));
            _logger?.LogInformation($"IgnoreAllButtons: {IgnoreAllButtons} IgnoredButtons: {ignoredButtonsList}");
            string ignoredAxesList = String.Join(",", IgnoredAxes.Select(x => x.ToString()));
            _logger?.LogInformation($"IgnoreAllAxes: {IgnoreAllAxes} IgnoredAxes: {ignoredAxesList}");
            string mappedButtonList = String.Join(" ", MappedButtons);
            _logger?.LogInformation($"MappedButtons: {mappedButtonList}");
            string mappedAxesList = String.Join(" ", MappedAxes);
            _logger?.LogInformation($"MappedAxes: {mappedAxesList}");
        }

        private HashSet<byte> ParseList(string list) {
            if (string.IsNullOrWhiteSpace(list))
                return new HashSet<byte>();

            // 0,5,6            
            string[] values = list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return values.Where(x => byte.TryParse(x, out byte test)).Select(x => byte.Parse(x)).ToHashSet();            
        }

        private Dictionary<byte, byte> ParseMap(string mapString) {
            if (string.IsNullOrWhiteSpace(mapString))
                return new Dictionary<byte, byte>();;
            
            // A=B,C=D
            Dictionary<byte, byte> map = new Dictionary<byte, byte>();
            string[] values = mapString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);            
            foreach(string value in values) {
                string[] bits = value.Split('=', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (bits.Length == 2 && byte.TryParse(bits[0], out byte t) && byte.TryParse(bits[1], out byte t2)) {
                    map[byte.Parse(bits[0])] = byte.Parse(bits[1]);
                }                
            }
            return map;
        }

        private string ToConfigMap(Dictionary<byte, byte> map) {
            string mapString = "";
            foreach(KeyValuePair<byte, byte> value in map) {
                mapString += $"{value.Key}={value.Value},";
            }
            if (mapString.EndsWith(",")) { mapString = mapString.TrimEnd(','); }
            return mapString;
        }
    }
}