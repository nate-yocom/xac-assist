using Nfw.Linux.Joystick.Smart;

namespace XacAssist {    
    public class XacAssistWorker : BackgroundService
    {
        private readonly ILogger<XacAssistWorker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;

        private const int OUTPUT_AXIS_MIN = -127;
        private const int OUTPUT_AXIS_MAX = 127;
        private const float INPUT_AXIS_MIN = -32768;
        private const float INPUT_AXIS_MAX = 32768;

        private const float DEFAULT_FIRE_THRESHOLD = 0.80f;     // Fire >= 80%
        private const float DEFAULT_RESET_THRESHOLD = 0.15f;    // Reset once <= 10%

        private const sbyte RESET_VALUE = 0x00;        

        private Dictionary<byte, bool> _currentlyFiredAxis = new Dictionary<byte, bool>();        
        private object _mutex = new object();
        private ushort _buttonState = 0x0000;
        private byte[] _buffer = new byte[11];
        private Dictionary<byte, byte> _buttonMapping = new Dictionary<byte, byte>();
        private HashSet<byte> _ignoreButtons = new HashSet<byte>();
        
        public XacAssistWorker(ILogger<XacAssistWorker> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration) {
            _logger = logger;
            _serviceScopeFactory = scopeFactory;
            _configuration = configuration;
        }

        private CancellationTokenSource _internalStopToken = new CancellationTokenSource();
        private CancellationTokenSource _shutdownComplete = new CancellationTokenSource();
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {        
            using(IServiceScope scope = _serviceScopeFactory.CreateScope()) {                                    
                // Endless loop until stopped...
                _logger.LogInformation("ExecuteAsync() running"); 

                string readDevice = _configuration["Devices:Input"] ?? "/dev/input/js0";
                string writeDevice = _configuration["Devices:Output"] ?? "/dev/hidg0";
                bool fireAndReset = bool.Parse(_configuration["Options:FireAndReset"] ?? "True");
                int waitToReset = int.Parse(_configuration["Options:WaitToResetMs"] ?? "10");
                float fireThreshold = float.Parse(_configuration["Options:FireThreshold"] ?? DEFAULT_FIRE_THRESHOLD.ToString());
                float resetThreshold = float.Parse(_configuration["Options:ResetThreshold"] ?? DEFAULT_RESET_THRESHOLD.ToString());
                string buttonMap = _configuration["Options:ButtonMap"] ?? "1=4,0=5";
                string ignoreButtons = _configuration["Options:IgnoreButtons"] ?? "";
                
                _logger.LogInformation($"Piping {readDevice} => {writeDevice}");
                _logger.LogInformation($"FireAndReset => {fireAndReset} WaitToReset => {waitToReset}");
                _logger.LogInformation($"FireThreshold => {fireThreshold} ResetThreshold => {resetThreshold}");
                _logger.LogInformation($"ButtonMap: {buttonMap}");
                _logger.LogInformation($"IgnoreButtons: {ignoreButtons}");
                ParseButtonMapOption(buttonMap);
                ParseButtonIgnoreOption(ignoreButtons);

                using(Joystick joystick = new Joystick(readDevice, ButtonEventTypes.Press | ButtonEventTypes.Release)) {

                    joystick.DefaultButtonSettings = new ButtonSettings() { 
                        LongPressMinimumDurationMilliseconds = 500
                    };

                    joystick.ButtonCallback = (j, button, eventType, pressed, elapsedTime) => {
                        lock(_mutex) {
                            byte actualButton = MapButton(button);
                            _logger.LogDebug($"Button {button} {eventType} => Maps to {actualButton} Ignored => {_ignoreButtons.Contains(button)} [{elapsedTime}]");

                            if (!_ignoreButtons.Contains(button)) {
                                UpdateButton(writeDevice, actualButton, eventType == ButtonEventTypes.Press);
                            }
                        }
                    };

                    joystick.AxisCallback = (j, axis, value, elapsedTime) => {
                        lock(_mutex) {
                            if (axis != 0 && axis != 1) {
                                UpdateAxis(writeDevice, axis, ScaleAxisValue(value));                            
                            } else {                            
                                float currentValuePercentage = Math.Abs(value) / INPUT_AXIS_MAX;
                                if (IsCurrentlyFired(axis) && currentValuePercentage <= resetThreshold) {
                                    if (!fireAndReset) {                                        
                                        UpdateAxis(writeDevice, axis, RESET_VALUE);
                                    }
                                    _logger.LogDebug($"Hit {axis} RESET at value: {value} [{currentValuePercentage * 100.0f}%]");
                                    SetCurrentlyFired(axis, false);
                                } else if (!IsCurrentlyFired(axis) && currentValuePercentage >= fireThreshold) {
                                    _logger.LogDebug($"Axis {axis} FIRE at value: {value} [{currentValuePercentage * 100.0f}%]");
                                    UpdateAxis(writeDevice, axis, value < 0 ? (sbyte) OUTPUT_AXIS_MIN : (sbyte) OUTPUT_AXIS_MAX);
                                    if (fireAndReset) {                                        
                                        Thread.Sleep(waitToReset);
                                        _logger.LogDebug($"Axis {axis} auto-RESET after FIRING");
                                        UpdateAxis(writeDevice, axis, RESET_VALUE);
                                    }
                                    SetCurrentlyFired(axis, true);
                                } else {
                                    _logger.LogTrace($"Axis {axis} ignoring {value} => CurrentlyFired: {IsCurrentlyFired(axis)}");
                                }                                     
                            }
                        }       
                    };

                    joystick.ConnectedCallback = (j, c) => {
                        _logger.LogDebug($"{j.DeviceName} => Connected[{c}]");
                    };                                  

                    while (!stoppingToken.IsCancellationRequested && !_internalStopToken.Token.IsCancellationRequested) {                                           
                        await Task.Delay(1000, stoppingToken);
                    }            
                            
                    _logger.LogInformation("ExecuteAsync() signaled to stop");
                    _shutdownComplete.Cancel();
                }
            }
        }

        private void ParseButtonMapOption(string mapString) {
            if (string.IsNullOrWhiteSpace(mapString))
                return;
            
            // A=B,C=D
            string[] maps = mapString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach(string map in maps) {
                string[] bits = map.Split('=', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                _buttonMapping[byte.Parse(bits[0])] = byte.Parse(bits[1]);
            }
        }

        private void ParseButtonIgnoreOption(string buttonString) {
            if (string.IsNullOrWhiteSpace(buttonString))
                return;
            
            // 0,5,6
            string[] buttons = buttonString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach(string button in buttons) {
                _ignoreButtons.Add(byte.Parse(button));
            }
        }

        private byte MapButton(byte inputButton) {
            return _buttonMapping.ContainsKey(inputButton) ? _buttonMapping[inputButton] : inputButton;
        }
        
        private bool IsCurrentlyFired(byte axis) {
            lock(_mutex) {
                if (!_currentlyFiredAxis.ContainsKey(axis)) {
                    _currentlyFiredAxis[axis] = false;
                    return false;
                }

                return _currentlyFiredAxis[axis];
            }
        }

        private void SetCurrentlyFired(byte axis, bool value) {
            lock (_mutex) {
                _currentlyFiredAxis[axis] = value;
            }
        }

        private sbyte ScaleAxisValue(short inputValue) {
            sbyte result = 0;
            if (inputValue != 0) {            
                float percentOfMax = Math.Abs(inputValue) / INPUT_AXIS_MAX;
                int newValue = (int) (percentOfMax * (float) (OUTPUT_AXIS_MAX + 1));            
                if (inputValue < 0) newValue *= -1;
                newValue = Math.Clamp(newValue, OUTPUT_AXIS_MIN, OUTPUT_AXIS_MAX);
                
                result = (sbyte) newValue;
            }
            _logger.LogTrace($"Scaled {inputValue} => {result}");
            return result;
        }

        private void FireBuffer(string outDevice) {
            try {
                using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(outDevice))) {
                    writer.Write(_buffer, 0, _buffer.Length);
                    writer.Flush();
                }
            } catch(Exception ex) {
                _logger?.LogWarning($"Exception while writing buffer [{Convert.ToHexString(_buffer)}]: {ex}");
            }
        }

        private void UpdateAxis(string outDevice, byte axis, sbyte value) {            
            _buffer[2 + axis] = (byte) value;
            FireBuffer(outDevice);
        }

        private void UpdateButton(string outDevice, byte button, bool pressed) {            
            if (pressed) {
                _buttonState |= (ushort) (0x01 << button);
            } else {
                _buttonState &= (ushort) (~(0x01 << button));
            }                  
            BitConverter.TryWriteBytes(_buffer, _buttonState);                              
            FireBuffer(outDevice);                        
        }

        public override async Task StopAsync(CancellationToken cancellationToken) {
            // Tell ourselves to stop
            _internalStopToken.Cancel();
            _logger.LogInformation("stopping");

            // Now wait for ack from ExecutAsync()
            while (!_shutdownComplete.IsCancellationRequested) {
                _logger.LogInformation("Waiting for ExecutAsync to cleanup");
                await Task.Delay(1000);
            }
        }
    }
}