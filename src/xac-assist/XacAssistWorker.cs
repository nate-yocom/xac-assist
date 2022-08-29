using Nfw.Linux.Joystick.Smart;

namespace XacAssist {    
    public class XacAssistWorker : BackgroundService
    {
        private readonly ILogger<XacAssistWorker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;

        public XacAssistWorker(ILogger<XacAssistWorker> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration) {
            _logger = logger;
            _serviceScopeFactory = scopeFactory;
            _configuration = configuration;
        }

        private CancellationTokenSource _internalStopToken = new CancellationTokenSource();
        private CancellationTokenSource _shutdownComplete = new CancellationTokenSource();
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {        
            using(IServiceScope scope = _serviceScopeFactory.CreateScope()) {                                    
                // Endless loop until stopped...
                _logger.LogInformation("ExecuteAsync() running"); 

                string readDevice = _configuration["Devices:Input"] ?? "/dev/input/js0";
                string writeDevice = _configuration["Devices:Output"] ?? "/dev/hidg0";
                _logger.LogInformation($"Piping {readDevice} => {writeDevice}");

                using(Joystick joystick = new Joystick(readDevice, ButtonEventTypes.All)) {

                    joystick.DefaultButtonSettings = new ButtonSettings() { 
                        LongPressMinimumDurationMilliseconds = 500
                    };

                    joystick.ButtonCallback = (j, button, eventType, pressed, elapsedTime) => {
                        _logger.LogDebug($"{j.DeviceName} => Button[{button}] => {eventType} [Current: {pressed} Elapsed: {elapsedTime}]");
                    };

                    joystick.AxisCallback = (j, axis, value, elapsedTime) => {
                        _logger.LogDebug($"{j.DeviceName} => Axis[{axis}] => {value} [Elapsed: {elapsedTime}]");
                    };

                    joystick.ConnectedCallback = (j, c) => {
                        _logger.LogDebug($"{j.DeviceName} => Connected[{c}]");
                    };                                  

                    while (!stoppingToken.IsCancellationRequested && !_internalStopToken.Token.IsCancellationRequested)
                    {                                           
                        await Task.Delay(1000, stoppingToken);
                    }            
                            
                    _logger.LogInformation("ExecuteAsync() signaled to stop");
                    _shutdownComplete.Cancel();
                }
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