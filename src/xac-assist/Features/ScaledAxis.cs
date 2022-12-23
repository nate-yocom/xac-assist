using Nfw.Linux.Joystick.Smart;
using Nfw.Linux.Hid.Joystick;

using XacAssist.Renderer;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

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

        // TBD: For now this is hard coded based on the background image we're using and an 800x480 screen.
        private static readonly PointF DEFAULT_TEXT_LOCATION = new PointF(425.0f, 90.0f);
        private static readonly Point IMAGE_OVERLAY_POSITION = new Point(0, 240);

        // We load this once and hold onto it to avoid repeatedly having to load it etc
        private Image<Bgr565> _scaleHintImage = Image.Load<Bgr565>("data/images/background_bottom.png");

        public override void TickFrame(Image frame) {
            // If we are enabled, we label MODE as scaled, and then include our hints for how to change the current scale
            if (Enabled) {
                frame.Mutate(f => {
                    f.DrawImage(_scaleHintImage, IMAGE_OVERLAY_POSITION, 1.0f);
                    f.DrawText($"SCALE AXIS BY {ScaleAmount.ToString("P0")}", FontManager.GetFont(FontStyle.Bold), Color.Blue, DEFAULT_TEXT_LOCATION);                    
                });
            }                        
        }
        
        public override FeatureFilterAction ButtonFilter(ref byte buttonId, ButtonEventTypes eventType, ref bool pressed, TimeSpan elapsed) {
            if (Enabled) {
                // Hack for now, in the absence of a UI for configuration... button 10 means -.05, button 11 means + 0.05
                if (eventType == ButtonEventTypes.Press) {
                    if (buttonId == 10) {
                        ScaleAmount = Math.Max(0.05f, ScaleAmount - 0.05f);
                        _logger.LogDebug($"Decremented scale amount to: {ScaleAmount}");
                    } else if (buttonId == 11) {
                        ScaleAmount = Math.Min(1.0f, ScaleAmount + 0.05f);                        
                        _logger.LogDebug($"Incremented scale amount to: {ScaleAmount}");
                    }
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