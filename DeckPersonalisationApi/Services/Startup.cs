using System.Net.Mail;
using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Services;

public class Startup : BackgroundService
{
    private readonly ILogger<Startup> _logger;
    private IServiceProvider _services;
    private AppConfiguration _conf;
    private Timer? _premiumCheck;
    private DiscordBot _bot;

    public Startup(ILogger<Startup> logger, IServiceProvider provider, AppConfiguration conf)
    {
        _logger = logger;
        _services = provider;
        _conf = conf;
        _bot = new(_conf);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _services.CreateScope())
        {
            var ctx = 
                scope.ServiceProvider
                    .GetRequiredService<ApplicationContext>();

            await ctx.Database.EnsureCreatedAsync(stoppingToken);
        }

        _premiumCheck = new Timer(_ => InfrequentTask(), null, TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(_conf.InfrequentBackgroundServiceFrequencyMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation($"Running background service at {DateTime.Now:HH:mm:ss}");
            await RemoveExpiredBlobs();
            await WriteBlobDownloads();
            await UpdateStars();
            await Task.Delay(TimeSpan.FromMinutes(_conf.BackgroundServiceFrequencyMinutes), stoppingToken);
        }

        await _premiumCheck.DisposeAsync();
    }

    private async Task RemoveExpiredBlobs()
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

    private async Task WriteBlobDownloads()
    {
        using (var scope = _services.CreateScope())
        {
            var blobService = scope.ServiceProvider.GetRequiredService<BlobService>();
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
            var cache = taskService.RolloverRegisteredDownloads();
            await Task.Delay(100);
            blobService.WriteDownloadCache(cache);
            
            // Sneaky extra
            taskService.ClearOldTasks();
        }
    }

    private async Task UpdateStars()
    {
        using (var scope = _services.CreateScope())
        {
            var cssThemeService = scope.ServiceProvider.GetRequiredService<ThemeService>();
            cssThemeService.UpdateStars();
        }
    }

    private async void InfrequentTask()
    {
        _logger.LogInformation($"Running infrequent background service at {DateTime.Now:HH:mm:ss}");
        await UpdatePremiumStatus();
    } 

    private async Task UpdatePremiumStatus()
    {
        long count = await _bot.Cache();
        _logger.LogInformation($"Cached {count} discord users");
    }
}