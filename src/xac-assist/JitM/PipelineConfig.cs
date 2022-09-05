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


        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        public PipelineConfig(ILogger<PipelineConfig> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
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
            InputDevice = _configuration.GetValue<string>("Devices:Input", InputDevice);
            OutputDevice = _configuration.GetValue<string>("Devices:Output", OutputDevice);            
            WaitToReset = _configuration.GetValue<int>("Options:WaitToResetMs", WaitToReset);
            FireThreshold = _configuration.GetValue<float>("Options:FireThreshold", FireThreshold);
            ResetThreshold = _configuration.GetValue<float>("Options:ResetThreshold", ResetThreshold);
            IgnoreAllButtons = _configuration.GetValue<bool>("Options:IgnoreAllButtons", IgnoreAllButtons);
            IgnoreAllAxes = _configuration.GetValue<bool>("Options:IgnoreAllAxes", IgnoreAllAxes);
                        
            FireAndResetAxes = ParseList(_configuration.GetValue<string>("Options:FireAndResetAxes", "0,1"));
            IgnoredButtons = ParseList(_configuration.GetValue<string>("Options:IgnoreButtons", ""));
            IgnoredAxes = ParseList(_configuration.GetValue<string>("Options:IgnoreAxes", ""));
            
            MappedButtons = ParseMap(_configuration.GetValue<string>("Options:ButtonMap", ""));
            LogConfiguration();
        }

        private void LogConfiguration() {
            _logger.LogInformation($"InputDevice: {InputDevice} OutputDevice: {OutputDevice}");
            string fireAxes = String.Join(",", FireAndResetAxes.Select(x => x.ToString()));
            _logger.LogInformation($"FireAndResetAxes: {fireAxes} WaitToReset: {WaitToReset} FireThreshold: {FireThreshold} ResetThreshold: {ResetThreshold}");
            string ignoredButtonsList = String.Join(",", IgnoredButtons.Select(x => x.ToString()));
            _logger.LogInformation($"IgnoreAllButtons: {IgnoreAllButtons} IgnoredButtons: {ignoredButtonsList}");
            string ignoredAxesList = String.Join(",", IgnoredAxes.Select(x => x.ToString()));
            _logger.LogInformation($"IgnoreAllAxes: {IgnoreAllAxes} IgnoredAxes: {ignoredAxesList}");
            string mappedButtonList = String.Join(" ", MappedButtons);
            _logger.LogInformation($"MappedButtons: {mappedButtonList}");
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
    }
}