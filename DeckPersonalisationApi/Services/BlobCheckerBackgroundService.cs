namespace DeckPersonalisationApi.Services;

public class BlobCheckerBackgroundService : BackgroundService
{
    private readonly ILogger<BlobCheckerBackgroundService> _logger;
    private IServiceProvider _services;
    private IConfiguration _conf;
    
    public TimeSpan BlobTTLMinutes => TimeSpan.FromMinutes(int.Parse(_conf["Config:BlobTTLMinutes"]!));
    
    public BlobCheckerBackgroundService(ILogger<BlobCheckerBackgroundService> logger, IServiceProvider provider, IConfiguration conf)
    {
        _logger = logger;
        _services = provider;
        _conf = conf;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Run(RemoveExpiredBlobs);
            await Task.Delay(BlobTTLMinutes, stoppingToken);
        }
    }

    private void RemoveExpiredBlobs()
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
}