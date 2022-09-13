using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using System.Diagnostics;

namespace XacAssist.JitM {

    public class Pipeline : IPipeline {

        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IPipelineConfig _configuration;

        private const float INPUT_AXIS_MIN = -32768;
        private const float INPUT_AXIS_MAX = 32768;
        private const sbyte RESET_VALUE = 0x00;
        
        private Joystick? _inputJoystick;
        private SimpleJoystick? _outputJoystick;
        private Stopwatch _stopwatch = Stopwatch.StartNew();
        private Dictionary<byte, TimeSpan> _currentlyFiredAxis = new Dictionary<byte, TimeSpan>();
        private object _mutex = new object();
        
        public Pipeline(ILogger<Pipeline> logger, ILoggerFactory loggerFactory, IPipelineConfig configuration) {
            _logger = logger;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        // Reads configuration and sets up JS listen hooks
        public void Start() {
            _logger.LogDebug($"Start()");
            _configuration.ReadConfiguration();
            _outputJoystick = new SimpleJoystick(_configuration.OutputDevice,_loggerFactory.CreateLogger(_configuration.OutputDevice));
            _inputJoystick = new Joystick(_configuration.InputDevice, _loggerFactory.CreateLogger(_configuration.InputDevice), ButtonEventTypes.Press | ButtonEventTypes.Release);            
            _inputJoystick.ButtonCallback = ButtonCallback;
            _inputJoystick.AxisCallback = AxisCallback;
        }

        public void Tick() {
            if (_configuration.AllowAxisHoldToFlow) {
                lock(_mutex) {
                    foreach(KeyValuePair<byte, TimeSpan> kv in _currentlyFiredAxis) {
                        if ((_stopwatch.Elapsed - kv.Value).TotalMilliseconds >= _configuration.AxisHoldToFlowHoldTimeMilliseconds) {
                            _logger.LogTrace($"Tick firing axis as a result of duration {kv.Key} => {_stopwatch.Elapsed - kv.Value} => {_inputJoystick!.AxisValue(kv.Key)}");
                            _outputJoystick?.UpdateAxis(kv.Key, ScaleAxisValue(_inputJoystick!.AxisValue(kv.Key)));
                        }
                    }
                }
            }
        }

        // Removes JS hooks and any other cleanup
        public void Stop() {
            _logger.LogDebug($"Stop()");
            if (_inputJoystick != null) {
                _inputJoystick.ButtonCallback = null;
                _inputJoystick.AxisCallback = null;
                _inputJoystick.ConnectedCallback = null;
                _inputJoystick.Dispose();
            }
            _outputJoystick = null;
        }

        private void ButtonCallback(Joystick joystick, byte buttonId, ButtonEventTypes eventType, bool pressed, TimeSpan elapsed) {
            lock(_mutex) {
                _logger.LogTrace($"{joystick.Device} [{joystick.DeviceName}] => Button[{buttonId}->{_configuration.MapButtonIfMapped(buttonId)}]:{eventType} Pressed=>{pressed} [{elapsed}]");

                if (_configuration.IgnoreAllButtons) return;

                byte actualButton = _configuration.MapButtonIfMapped(buttonId);
                if (!_configuration.IsIgnoreButton(actualButton)) return;
                
                _outputJoystick?.UpdateButton(actualButton, eventType == ButtonEventTypes.Press);
            }
        }

        private void AxisCallback(Joystick joystick, byte axisId, short value, TimeSpan elapsed) {
            lock(_mutex) {
                _logger.LogTrace($"{joystick.Device} [{joystick.DeviceName}] => Axis[{axisId}->{_configuration.MapAxisIfMapped(axisId)}]:{value} [{elapsed}]");

                if (_configuration.IgnoreAllAxes) return;

                axisId = _configuration.MapAxisIfMapped(axisId);

                if (_configuration.IsIgnoreAxis(axisId)) return;

                // If not in our fire and reset list, we just pass through
                if (!_configuration.FireAndResetAxes.Contains(axisId)) {
                    _outputJoystick?.UpdateAxis(axisId, ScaleAxisValue(value));
                } else {
                    float currentValuePercentage = Math.Abs(value) / INPUT_AXIS_MAX;

                    if (IsAxisCurrentlyFired(axisId) && currentValuePercentage <= _configuration.ResetThreshold) {
                        // Fired, but moving back toward zero ==> Reset
                        _logger.LogDebug($"Hit {axisId} RESET at value: {value} [{currentValuePercentage * 100.0f}%]");
                        SetAxisNotFired(axisId);
                        _outputJoystick?.UpdateAxis(axisId, RESET_VALUE);
                    } else if (!IsAxisCurrentlyFired(axisId) && currentValuePercentage >= _configuration.FireThreshold) {
                        // Not fired, but moved far enough to trigger? ==> Emit a fire, delay, then 0.
                        _logger.LogDebug($"Hit {axisId} FIRE at value {value} [{currentValuePercentage * 100.0f}%]");
                        _outputJoystick?.UpdateAxis(axisId, value < 0 ? SimpleJoystick.MIN_AXIS_VALUE : SimpleJoystick.MAX_AXIS_VALUE);
                        Thread.Sleep(_configuration.WaitToReset);
                        _logger.LogDebug($"Axis {axisId} auto-RESET after FIRING");
                        _outputJoystick?.UpdateAxis(axisId, RESET_VALUE);
                        SetAxisCurrentlyFired(axisId);
                    }
                }
            } 
        }

        private void ConnectedCallback(Joystick joystick, bool connected) {
            _logger.LogInformation($"{joystick.DeviceName} => Connected[{connected}]");
        }

        private bool IsAxisCurrentlyFired(byte axis) {
            return _currentlyFiredAxis.ContainsKey(axis);
        }

        private void SetAxisCurrentlyFired(byte axis) {
            _currentlyFiredAxis[axis] = _stopwatch.Elapsed;
        }

        private void SetAxisNotFired(byte axis) {
            _currentlyFiredAxis.Remove(axis);
        }

        private TimeSpan AxisFiredDuration(byte axis) {
            return (IsAxisCurrentlyFired(axis) ? (_stopwatch.Elapsed - _currentlyFiredAxis[axis]) : TimeSpan.MinValue);
        }

        private sbyte ScaleAxisValue(short inputValue) {
            sbyte result = 0;
            if (inputValue != 0) {            
                float percentOfMax = Math.Abs(inputValue) / INPUT_AXIS_MAX;
                int newValue = (int) (percentOfMax * (float) (SimpleJoystick.MAX_AXIS_VALUE + 1));
                if (inputValue < 0) newValue *= -1;
                newValue = Math.Clamp(newValue, SimpleJoystick.MIN_AXIS_VALUE, SimpleJoystick.MAX_AXIS_VALUE);
                
                result = (sbyte) newValue;
            }
            _logger.LogTrace($"Scaled {inputValue} => {result}");
            return result;
        }
    }
}