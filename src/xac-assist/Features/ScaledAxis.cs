using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using System.Diagnostics;

namespace XacAssist.Features {

    public class ScaledAxis : Feature {
        public override string Name { get { return "Scaled Axis"; } }

        public ScaledAxis(ILoggerFactory loggerFactory, Joystick inputJoystick, SimpleJoystick outputJoystick) : 
            base(loggerFactory, inputJoystick, outputJoystick) {
        }
                
        public override void Start() {
        }

        public override void Tick() {
        }
        
        public override void Stop() {
        }
        
        public override FeatureFilterAction ButtonFilter(ref byte buttonId, ButtonEventTypes eventType, ref bool pressed, TimeSpan elapsed) {
            // We don't do anything with buttons
            return FeatureFilterAction.Continue;
        }
                
        public override FeatureFilterAction AxisFilter(ref byte axisId, ref short value, TimeSpan elapsed) {
            // When we're enabled, we scale and modify the axis value, then let it flow
            return FeatureFilterAction.Continue;
        }
    }
}