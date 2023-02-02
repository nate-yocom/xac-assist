using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using XacAssist.Pipeline;
using XacAssist.Features;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace XacAssist.JitM {

    public class JitMPipeline : IPipeline {

        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;        
        private readonly IConfiguration _configuration;

        private Joystick? _inputJoystick;
        private SimpleJoystick? _outputJoystick;        
        private object _mutex = new object();
        private Stopwatch _lastInputTimer = Stopwatch.StartNew();


        private List<Feature> _features = new List<Feature>();
        public ReadOnlyCollection<Feature> Features { get { return _features.AsReadOnly(); }}

        
        public JitMPipeline(ILoggerFactory loggerFactory, IConfiguration configuration) {
            _logger = loggerFactory.CreateLogger<JitMPipeline>();
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        // Reads configuration and sets up JS listen hooks
        public void Start() {
            _logger.LogDebug($"Start()");            
            _outputJoystick = new SimpleJoystick(_configuration["OutputDevice"], _loggerFactory.CreateLogger(_configuration["OutputDevice"]));
            _inputJoystick = new Joystick(_configuration["InputDevice"], _loggerFactory.CreateLogger(_configuration["InputDevice"]), ButtonEventTypes.Press | ButtonEventTypes.Release | ButtonEventTypes.LongPress);
            _inputJoystick.DefaultButtonSettings.LongPressMinimumDurationMilliseconds = 1000;
            _inputJoystick.ButtonCallback = ButtonCallback;
            _inputJoystick.AxisCallback = AxisCallback;

            _features.Clear();
            _features.Add(new FullPassThru(_loggerFactory, _inputJoystick, _outputJoystick) { Enabled = false, ToggleButton = 6 });
            // Single fire and Scaled axis both want to muck with axis - and are mutually exclusive - so .. they use the same toggle, but 
            //  different initial states.  We start with the single fire feature on, toggling that off will toggle the scaled axis ON.
            _features.Add(new SingleFireAxis(_loggerFactory, _inputJoystick, _outputJoystick) { Enabled = true, ToggleButton = 7 });
            _features.Add(new ScaledAxis(_loggerFactory, _inputJoystick, _outputJoystick) { Enabled = false, ToggleButton = 7 });
            _features.Add(new IgnoreButtons(_loggerFactory, _inputJoystick, _outputJoystick) { Enabled = true, ToggleButton = 8 });

            foreach(Feature feature in _features) {
                _logger.LogDebug($"Starting feature: {feature.Name} Enabled => {feature.Enabled} Toggle => {feature.ToggleButton}");
                feature.Start();
            }            
        }

        public Stopwatch TimeSinceLastInput()
        {
            return _lastInputTimer;
        }

        public void Tick() {
            lock(_mutex) {
                foreach(Feature feature in _features) {
                    feature.Tick();
                }
            }
        }

        // Removes JS hooks and any other cleanup
        public void Stop() {
            _logger.LogDebug($"Stop()");

            lock(_mutex) {
                foreach(Feature feature in _features) {
                    feature.Stop();
                }

                if (_inputJoystick != null) {
                    _inputJoystick.ButtonCallback = null;
                    _inputJoystick.AxisCallback = null;
                    _inputJoystick.ConnectedCallback = null;
                    _inputJoystick.Dispose();
                }
                _outputJoystick = null;
            }
        }

        private void ButtonCallback(Joystick joystick, byte buttonId, ButtonEventTypes eventType, bool pressed, TimeSpan elapsed) {
            lock(_mutex) {
                _lastInputTimer.Restart();
                _logger.LogTrace($"{joystick.Device} [{joystick.DeviceName}] => Button[{buttonId}]:{eventType} Pressed=>{pressed} [{elapsed}]");

                // Long press button 0 means dump state
                if (eventType == ButtonEventTypes.LongPress && buttonId == 0) {
                    _logger.LogDebug($"===== CURRENT FEATURE STATE ======");
                    foreach(Feature feature in _features) {
                        _logger.LogDebug($"  {feature.Name} Enabled=>{feature.Enabled} Toggle=>{feature.ToggleButton}");
                    }
                }

                // Long press means toggle
                if (eventType == ButtonEventTypes.LongPress && _features.Any(f => f.ToggleButton == buttonId)) {
                    foreach(Feature feature in _features.Where(f => f.ToggleButton == buttonId)) {
                        _logger.LogDebug($"Toggling {feature.Name} from {feature.Enabled} to {!feature.Enabled}");
                        feature.Enabled = !feature.Enabled;
                    }
                    // We always emit an NOT PRESSED then return, as this button press has done what it was meant to do.
                    _outputJoystick?.UpdateButton(buttonId, false);
                    return;
                }

                bool swallowed = false;
                foreach(Feature feature in _features) {
                    if(feature.Enabled) {
                        _logger.LogTrace($"===> {feature.Name} => Button[{buttonId}] Pressed=>{pressed}");
                        var action = feature.ButtonFilter(ref buttonId, eventType, ref pressed, elapsed);
                        _logger.LogTrace($"<=== {action} from {feature.Name} => Button[{buttonId}] Pressed=>{pressed}");
                        swallowed |= action == Feature.FeatureFilterAction.Swallowed || action == Feature.FeatureFilterAction.Stop;
                        if (action == Feature.FeatureFilterAction.Stop)
                            break;
                    }
                }

                if (!swallowed) {
                    _logger.LogTrace($"Updating output Button[{buttonId}] Pressed=>{pressed}");
                    _outputJoystick?.UpdateButton(buttonId, pressed);
                }
            }
        }

        private void AxisCallback(Joystick joystick, byte axisId, short value, TimeSpan elapsed) {
            lock(_mutex) {
                _lastInputTimer.Restart();
                _logger.LogTrace($"{joystick.Device} [{joystick.DeviceName}] => Axis[{axisId}]:{value} [{elapsed}]");

                bool swallowed = false;
                foreach(Feature feature in _features) {
                    if(feature.Enabled) {
                        _logger.LogTrace($"===> {feature.Name} => Axis[{axisId}]:{value}");
                        var action = feature.AxisFilter(ref axisId, ref value, elapsed);
                        _logger.LogTrace($"<===  {action} from {feature.Name} => Axis[{axisId}]:{value}");
                        swallowed |= action == Feature.FeatureFilterAction.Swallowed || action == Feature.FeatureFilterAction.Stop;
                        if (action == Feature.FeatureFilterAction.Stop)
                            break;
                    }
                }

                if (!swallowed) {
                    _logger.LogTrace($"Updating output Axis[{axisId}]:{value}");
                    _outputJoystick?.UpdateAxis(axisId, Utility.AxisHelper.ScaleAxisValue(value));
                }
            }
        }

        private void ConnectedCallback(Joystick joystick, bool connected) {
            _lastInputTimer.Restart();
            _logger.LogInformation($"{joystick.DeviceName} => Connected[{connected}]");
        }
    }
}