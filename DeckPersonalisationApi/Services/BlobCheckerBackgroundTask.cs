namespace DeckPersonalisationApi.Services;

public class BlobCheckerBackgroundTask : IHostedService, IDisposable
{
    private readonly ILogger<BlobCheckerBackgroundTask> _logger;
    private Timer? _timer = null;
    private IServiceProvider _services;
    private IConfiguration _conf;
    
    public TimeSpan BlobTTLMinutes => TimeSpan.FromMinutes(int.Parse(_conf["Config:BlobTTLMinutes"]!));
    
    public BlobCheckerBackgroundTask(ILogger<BlobCheckerBackgroundTask> logger, IServiceProvider provider, IConfiguration conf)
    {
        _logger = logger;
        _services = provider;
        _conf = conf;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlobCheckerBackgroundTask Service running.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero,
            BlobTTLMinutes);

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        using (var scope = _services.CreateScope())
        {
            var blobService = 
                scope.ServiceProvider
                    .GetRequiredService<BlobService>();

            int count = blobService.RemoveExpiredBlobs();
            _logger.LogInformation($"Deleted {count} unconfirmed blobs");
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlobCheckerBackgroundTask Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}