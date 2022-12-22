using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using XacAssist.Renderer;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

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

        // TBD: For now this is hard coded based on the background image we're using and an 800x480 screen.
        private static readonly PointF DEFAULT_TEXT_LOCATION = new PointF(585.0f, 20.0f);
        private static readonly PointF DEFAULT_MESSAGE_LOCATION = new PointF(475.0f, 100.0f);
        private static readonly Point ENABLED_OVERLAY_POSITION = new Point(0, 0);
        private Image<Bgr565> _passthroughEnabledImage = Image.Load<Bgr565>("data/images/background_passthrough_only.png");

        public override void TickFrame(Image frame) {
            frame.Mutate(f => {
                if (Enabled) {
                    f.DrawImage(_passthroughEnabledImage, ENABLED_OVERLAY_POSITION, 1.0f);
                    f.DrawText("ENGAGED", FontManager.GetFont(FontStyle.BoldItalic), Color.Green, DEFAULT_TEXT_LOCATION);

                    TextOptions options = new TextOptions(FontManager.GetFont(FontStyle.Italic)) {
                        Origin = DEFAULT_MESSAGE_LOCATION,
                        WrappingLength = 480 - DEFAULT_MESSAGE_LOCATION.Y - 40,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    f.DrawText(options, "Hold and release the passthrough button for a full second or more to return.", Color.Black);
                } else {
                    f.DrawText("OFF", FontManager.GetFont(), Color.Red, DEFAULT_TEXT_LOCATION);
                }
            });
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