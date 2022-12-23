using XacAssist.JitM;
using XacAssist.Pipeline;
using XacAssist.Renderer;

namespace XacAssist {    
    public class XacAssistWorker : BackgroundService
    {
        private readonly ILogger<XacAssistWorker> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IPipeline _pipeline;
        private readonly IConfiguration _configuration;
        private readonly FeatureStatusRenderer _featureRenderer;

        private const float INPUT_AXIS_MIN = -32768;
        private const float INPUT_AXIS_MAX = 32768;

        private const float DEFAULT_FIRE_THRESHOLD = 0.80f;     // Fire >= 80%
        private const float DEFAULT_RESET_THRESHOLD = 0.15f;    // Reset once <= 10%

        private const sbyte RESET_VALUE = 0x00;        

        private Dictionary<byte, bool> _currentlyFiredAxis = new Dictionary<byte, bool>();        
        private object _mutex = new object();
        private Dictionary<byte, byte> _buttonMapping = new Dictionary<byte, byte>();
        private HashSet<byte> _ignoreButtons = new HashSet<byte>();
        private HashSet<byte> _ignoreAxis = new HashSet<byte>();
        
        public XacAssistWorker(ILogger<XacAssistWorker> logger, IPipeline pipeline, 
                                IConfiguration configuration, ILoggerFactory loggerFactory, FeatureStatusRenderer featureRenderer) {
            _logger = logger;
            _pipeline = pipeline;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _featureRenderer = featureRenderer;
        }

        private CancellationTokenSource _internalStopToken = new CancellationTokenSource();
        private CancellationTokenSource _shutdownComplete = new CancellationTokenSource();
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // Endless loop until stopped...
            _logger.LogInformation("ExecuteAsync() running"); 

            _pipeline.Start();            
            _featureRenderer.Start(_pipeline.Features);
                
            while (!stoppingToken.IsCancellationRequested && !_internalStopToken.Token.IsCancellationRequested) {
                await Task.Delay(10, stoppingToken);
                _pipeline.Tick();
            }

            _pipeline.Stop();
            _featureRenderer.Stop();

            _logger.LogInformation("ExecuteAsync() signaled to stop");
            _shutdownComplete.Cancel();
        }

        public override async Task StopAsync(CancellationToken cancellationToken) {
            // Tell ourselves to stop
            _internalStopToken.Cancel();
            _logger.LogInformation("stopping");

            // Now wait for ack from ExecutAsync()
            while (!_shutdownComplete.IsCancellationRequested) {
                _logger.LogInformation("Waiting for ExecutAsync to cleanup");
                await Task.Delay(1000);
            }
        }
    }
}