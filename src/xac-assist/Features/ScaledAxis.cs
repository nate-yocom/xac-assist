using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using System.Diagnostics;

namespace XacAssist.Features {

    public class ScaledAxis : Feature {
        public override string Name { get { return "Scaled Axis"; } }

        public float ScaleAmount { get; set; } = 0.50f;


        private ILogger<ScaledAxis> _logger;

        public ScaledAxis(ILoggerFactory loggerFactory, Joystick inputJoystick, SimpleJoystick outputJoystick) : 
            base(loggerFactory, inputJoystick, outputJoystick) {
            _logger = loggerFactory.CreateLogger<ScaledAxis>();
        }
                
        public override void Start() {
        }

        public override void Tick() {
        }
        
        public override void Stop() {
        }
        
        public override FeatureFilterAction ButtonFilter(ref byte buttonId, ButtonEventTypes eventType, ref bool pressed, TimeSpan elapsed) {
            // Hack for now, in the absence of a UI for configuration... button 10 means -.05, button 11 means + 0.05
            if (eventType == ButtonEventTypes.Press) {
                if (buttonId == 10) {
                    ScaleAmount -= 0.10f;
                    _logger.LogDebug($"Decremented scale amount to: {ScaleAmount}");
                } else if (buttonId == 11) {
                    ScaleAmount += 0.10f;
                    _logger.LogDebug($"Incremented scale amount to: {ScaleAmount}");
                }
            }

            // We don't do anything with buttons
            return FeatureFilterAction.Continue;
        }
                
        public override FeatureFilterAction AxisFilter(ref byte axisId, ref short value, TimeSpan elapsed) {
            // When we're enabled, we scale and modify the axis value, then let it flow
            float scaledValue = ScaleAmount * ((float) value);
            value = Convert.ToInt16(scaledValue);
            return FeatureFilterAction.Continue;
        }
    }
}