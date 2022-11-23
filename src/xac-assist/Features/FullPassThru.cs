using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using System.Diagnostics;

namespace XacAssist.Features {

    public class FullPassThru : Feature {
        public override string Name { get { return "Pass Through"; } }

        public FullPassThru(ILoggerFactory loggerFactory, Joystick inputJoystick, SimpleJoystick outputJoystick) : 
            base(loggerFactory, inputJoystick, outputJoystick) {
        }
                
        public override void Start() {
        }

        public override void Tick() {
        }
        
        public override void Stop() {
        }
        
        public override FeatureFilterAction ButtonFilter(ref byte buttonId, ButtonEventTypes eventType, ref bool pressed, TimeSpan elapsed) {
            _outputJoystick?.UpdateButton(buttonId, pressed);
            return FeatureFilterAction.Stop;
        }
                
        public override FeatureFilterAction AxisFilter(ref byte axisId, ref short value, TimeSpan elapsed) {            
            _outputJoystick?.UpdateAxis(axisId, Utility.AxisHelper.ScaleAxisValue(value));
            return FeatureFilterAction.Stop;
        }
    }
}