using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using XacAssist.Renderer;

using System.Diagnostics;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

namespace XacAssist.Features {

    public class SingleFireAxis : Feature {
        private const float INPUT_AXIS_MIN = -32768;
        private const float INPUT_AXIS_MAX = 32768;
        private const sbyte RESET_VALUE = 0x00;

        private Stopwatch _stopwatch = Stopwatch.StartNew();
        private Dictionary<byte, TimeSpan> _currentlyFiredAxis = new Dictionary<byte, TimeSpan>();
        private ILogger<SingleFireAxis> _logger;
                
        // TBD: How to make these things configurable? (and features in general...)
        public HashSet<byte> FireAndResetAxes { get; set; } = new HashSet<byte>() { 0x00, 0x01 };        
        public int WaitToReset { get; set; } = 50;
        public float FireThreshold { get; set; } = 0.80f;
        public float ResetThreshold { get; set; } = 0.15f;
        public bool AllowAxisHoldToFlow { get; set; } = true;
        public int AxisHoldToFlowHoldTimeMilliseconds { get; set; } = 1500;

        
        public override string Name { get { return "Single Fire Axis"; } }

        public SingleFireAxis(ILoggerFactory loggerFactory, Joystick inputJoystick, SimpleJoystick outputJoystick) : 
            base(loggerFactory, inputJoystick, outputJoystick) {
                _logger = loggerFactory.CreateLogger<SingleFireAxis>();
        }
                
        public override void Start() {
        }

        public override void Tick() {
            if (AllowAxisHoldToFlow) {
                foreach(KeyValuePair<byte, TimeSpan> kv in _currentlyFiredAxis) {
                    if ((_stopwatch.Elapsed - kv.Value).TotalMilliseconds >= AxisHoldToFlowHoldTimeMilliseconds) {
                        _logger.LogTrace($"Tick firing axis as a result of duration {kv.Key} => {_stopwatch.Elapsed - kv.Value} => {_inputJoystick!.AxisValue(kv.Key)}");
                        _outputJoystick?.UpdateAxis(kv.Key, Utility.AxisHelper.ScaleAxisValue(_inputJoystick!.AxisValue(kv.Key)));
                    }
                }
            }
        }
        
        public override void Stop() {
        }

        // TBD: For now this is hard coded based on the background image we're using and an 800x480 screen.
        private static readonly PointF DEFAULT_TEXT_LOCATION = new PointF(425.0f, 90.0f);

        public override void TickFrame(Image frame) {
            // If we are enabled, we label MODE as scaled, and then include our hints for how to change the current scale
            if (Enabled) {
                frame.Mutate(f => {                    
                    f.DrawText($"SINGLE-FIRE AXIS", FontManager.GetFont(FontStyle.Bold), Color.Blue, DEFAULT_TEXT_LOCATION);
                });
            }                        
        }
        
        public override FeatureFilterAction ButtonFilter(ref byte buttonId, ButtonEventTypes eventType, ref bool pressed, TimeSpan elapsed) {
            // We don't do anything with buttons
            return FeatureFilterAction.Continue;
        }
                
        public override FeatureFilterAction AxisFilter(ref byte axisId, ref short value, TimeSpan elapsed) {            
            if (!FireAndResetAxes.Contains(axisId)) {
                // If not in our fire and reset list, we just pass through
                return FeatureFilterAction.Continue;
            } else {
                float currentValuePercentage = Math.Abs(value) / INPUT_AXIS_MAX;

                if (IsAxisCurrentlyFired(axisId) && currentValuePercentage <= ResetThreshold) {
                    // Fired, but moving back toward zero ==> Reset
                    _logger.LogDebug($"Hit {axisId} RESET at value: {value} [{currentValuePercentage * 100.0f}%]");
                    SetAxisNotFired(axisId);
                    _outputJoystick?.UpdateAxis(axisId, RESET_VALUE);
                } else if (!IsAxisCurrentlyFired(axisId) && currentValuePercentage >= FireThreshold) {
                    // Not fired, but moved far enough to trigger? ==> Emit a fire, delay, then 0.
                    sbyte send = value < 0 ? (sbyte) -127 : SimpleJoystick.MAX_AXIS_VALUE;
                    _logger.LogDebug($"Hit {axisId} FIRE at value {value} [{currentValuePercentage * 100.0f}%] - Sending: {send}");
                    _outputJoystick?.UpdateAxis(axisId, send);
                    Thread.Sleep(WaitToReset);
                    _logger.LogDebug($"Axis {axisId} auto-RESET after FIRING");
                    _outputJoystick?.UpdateAxis(axisId, RESET_VALUE);
                    SetAxisCurrentlyFired(axisId);
                }
            }

            // When we're enabled, we take over the axis, done and done.
            return FeatureFilterAction.Stop;
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
    }
}