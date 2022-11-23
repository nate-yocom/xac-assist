using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using System.Diagnostics;

namespace XacAssist.Features {

    public class IgnoreButtons : Feature {
        public override string Name { get { return "Ignore Buttons"; } }

        public IgnoreButtons(ILoggerFactory loggerFactory, Joystick inputJoystick, SimpleJoystick outputJoystick) : 
            base(loggerFactory, inputJoystick, outputJoystick) {
        }
                
        public override void Start() {
        }

        public override void Tick() {
        }
        
        public override void Stop() {
        }
        
        public override FeatureFilterAction ButtonFilter(ref byte buttonId, ButtonEventTypes eventType, ref bool pressed, TimeSpan elapsed) {
            // This is our lot in life...            
            return FeatureFilterAction.Stop;
        }
                
        public override FeatureFilterAction AxisFilter(ref byte axisId, ref short value, TimeSpan elapsed) {
            return FeatureFilterAction.Continue;
        }
    }
}