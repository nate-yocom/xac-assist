using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using System.Collections.ObjectModel;
using System.Diagnostics;

using Nfw.Linux.FrameBuffer;

using XacAssist.Pipeline;
using XacAssist.Features;

namespace XacAssist.Renderer {

    public class FeatureStatusRenderer {
        private const string DEFAULT_BACKGROUND_IMAGE = "data/images/background.png";        
        private const string DEFAULT_LOAD_SCREEN_BY_SYDNEY_YOCOM = "data/images/slaythespireyeahhhwithsignature.png";
        private const int LOAD_TIME_MILLISECONDS = 5000;
        private const int BLANK_AFTER_INACTIVE_TIME_SECONDS = 10;

        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<FeatureStatusRenderer> _logger;
        private readonly RawFrameBuffer? _frameBuffer;
        private ReadOnlyCollection<Feature>? _features;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _tickTask;
        private bool _blanked = false;
        private IPipeline? _pipeline = null;

        public FeatureStatusRenderer(ILoggerFactory loggerFactory, IConfiguration configuration) {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<FeatureStatusRenderer>();
            _configuration = configuration;

            // If we have a framebuffer, let's try to use it.            
            var frameBufferDevice = configuration["OutputFrameBuffer"];
            if (File.Exists(frameBufferDevice)) {
                try {
                    _logger.LogTrace($"Attempting to use {frameBufferDevice} for framebuffer output");
                    _frameBuffer = new RawFrameBuffer(frameBufferDevice, _loggerFactory.CreateLogger(frameBufferDevice), true);                    
                    _frameBuffer.Clear();                                        
                } catch(Exception ex) {
                    _logger.LogError($"Unable to use {frameBufferDevice} for framebuffer output, proceeding without display => {ex.Message}");
                    _frameBuffer = null;
                }
            }            
        }

        public void Start(IPipeline pipeline) {
            _features = pipeline.Features;
            _pipeline = pipeline;
            _tickTask = Task.Factory.StartNew(() => Tick(), TaskCreationOptions.LongRunning);
        }

        public void Stop() {
            _cancellationTokenSource.Cancel();
            _tickTask?.Wait();
        }

        private void Tick() {
            if (_frameBuffer == null) return;
            
            try {
                // Just in case.
                _frameBuffer.UnBlank();

                // Re-use the same byte[] always, to avoid re-alloc overhead
                byte[] pixelBytes = new byte[_frameBuffer.PixelWidth * _frameBuffer.PixelHeight * (_frameBuffer.PixelDepth / 8)];

                if (File.Exists(DEFAULT_LOAD_SCREEN_BY_SYDNEY_YOCOM)) {
                    using(Image<Bgr565> loadScreen = Image.Load<Bgr565>(DEFAULT_LOAD_SCREEN_BY_SYDNEY_YOCOM)) {
                        loadScreen.CopyPixelDataTo(pixelBytes);

                        // For the first LOAD_TIME_MILLISECONDS of time, we want to paint repeatedly, to overwrite the console
                        //  that sometimes shows up.
                        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
                        while(timer.ElapsedMilliseconds < LOAD_TIME_MILLISECONDS) {                                            
                            _frameBuffer.WriteRaw(pixelBytes);
                            Thread.Sleep(10);
                        }
                    }
                }

                _pipeline?.TimeSinceLastInput().Restart();

                // Load a fresh copy of the background image
                using(Image<Bgr565> image = Image.Load<Bgr565>(DEFAULT_BACKGROUND_IMAGE)) {
                    // Make sure it fits the screen    
                    image.Mutate(x =>
                    {
                        x.Resize(_frameBuffer.PixelWidth, _frameBuffer.PixelHeight);                        
                    });

                    // Find and hold onto the Passthrough feature, so we can differentiate modes
                    Feature passThrough = _features!.Where(f => f is FullPassThru).First();
                                        
                    while(!_cancellationTokenSource.Token.IsCancellationRequested) {
                        var forceUpdate = false;
                        if (_blanked && _pipeline?.TimeSinceLastInput().Elapsed.TotalSeconds < BLANK_AFTER_INACTIVE_TIME_SECONDS)
                        {
                            _blanked = false;
                            forceUpdate = true;
                            _frameBuffer.UnBlank();
                        }

                        // Any work to do?
                        if(forceUpdate || _features!.Any(f => f.Dirty)) {
                            forceUpdate = false;

                            // Clone the image
                            using(Image<Bgr565> frame = image.Clone()) {                                
                                // If passthrough is enabled, that is the only one that renders
                                if (passThrough.Enabled) {
                                    passThrough.TickFrame(frame);
                                } else {
                                    // Otherwise all features get a chance to render                                
                                    foreach(Feature feature in _features!) {
                                        feature.TickFrame(frame);
                                    }
                                }                            

                                // Display the result
                                frame.CopyPixelDataTo(pixelBytes);
                                _frameBuffer.WriteRaw(pixelBytes);
                            }

                            // Loop after a bit'o'rest                            
                            Thread.Sleep(10);
                        } else {              
                            // Has it been long enough to just blank?
                            if (_pipeline?.TimeSinceLastInput().Elapsed.TotalSeconds >= BLANK_AFTER_INACTIVE_TIME_SECONDS)
                            {
                                _blanked = true;
                                _frameBuffer.Blank();
                            }
                            Thread.Sleep(250);
                        }                                                
                    }
                }
            } catch(Exception ex) {
                _logger.LogError($"Error during feature status render: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }
}