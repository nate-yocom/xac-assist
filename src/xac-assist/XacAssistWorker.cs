namespace XacAssist {    
    public class XacAssistWorker : BackgroundService
    {
        private readonly ILogger<XacAssistWorker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public XacAssistWorker(ILogger<XacAssistWorker> logger, IServiceScopeFactory scopeFactory) {
            _logger = logger;
            _serviceScopeFactory = scopeFactory;
        }

        private CancellationTokenSource _internalStopToken = new CancellationTokenSource();
        private CancellationTokenSource _shutdownComplete = new CancellationTokenSource();
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {        
            using(IServiceScope scope = _serviceScopeFactory.CreateScope()) {                                    
                // Endless loop until stopped...
                _logger.LogInformation("ExecuteAsync() running"); 
                while (!stoppingToken.IsCancellationRequested && !_internalStopToken.Token.IsCancellationRequested)
                {                                           
                    await Task.Delay(1000, stoppingToken);
                }            
                            
                _logger.LogInformation("ExecuteAsync() signaled to stop");                
                _shutdownComplete.Cancel();     
            }
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