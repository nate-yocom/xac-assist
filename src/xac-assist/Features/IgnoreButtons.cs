using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using XacAssist.Renderer;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

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

        // TBD: For now this is hard coded based on the background image we're using and an 800x480 screen.
        private static readonly PointF DEFAULT_TEXT_LOCATION = new PointF(486.0f, 165.0f);

        public override void TickFrame(Image frame) {
            frame.Mutate(f => {
                f.DrawText(Enabled ? "IGNORED" : "ENABLED", FontManager.GetFont(Enabled ? FontStyle.Regular : FontStyle.BoldItalic), Enabled ? Color.Red : Color.Green, DEFAULT_TEXT_LOCATION);
            });
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