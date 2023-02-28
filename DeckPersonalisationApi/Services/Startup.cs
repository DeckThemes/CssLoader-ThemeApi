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
        
        await _bot.Cache();
        using (var scope = _services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            List<User> currentPremiumUsers =
                userService.GetUserByAnyPermission(Permissions.PremiumTier1 | Permissions.PremiumTier2 |
                                                   Permissions.PremiumTier3);

            List<DiscordApiUser> tier1 = _bot.GetUsersWithRoles(_conf.DiscordPremiumTier1Role.ToString());
            List<DiscordApiUser> tier2 = _bot.GetUsersWithRoles(_conf.DiscordPremiumTier2Role.ToString());
            List<DiscordApiUser> tier3 = _bot.GetUsersWithRoles(_conf.DiscordPremiumTier3Role.ToString());
            
            UpdateUserLists(currentPremiumUsers, tier1);
            UpdateUserLists(currentPremiumUsers, tier2);
            UpdateUserLists(currentPremiumUsers, tier3);
            
            // Remove old users
            currentPremiumUsers.ForEach(x => x.Permissions &= ~(Permissions.PremiumTier1 | Permissions.PremiumTier2 |
                                                               Permissions.PremiumTier3));
            userService.UpdateBulk(currentPremiumUsers);
            
            // Add new users
            long newCount = 0;
            newCount += UpdateTierUsers(tier1, Permissions.PremiumTier1, userService);
            newCount += UpdateTierUsers(tier2, Permissions.PremiumTier2, userService);
            newCount += UpdateTierUsers(tier3, Permissions.PremiumTier3, userService);
            
            _logger.LogInformation($"Removed tier from {currentPremiumUsers.Count} users, Added tier to {newCount} users");
        }
    }

    private int UpdateTierUsers(List<DiscordApiUser> tierList, Permissions tier, UserService userService)
    {
        List<User> tierUsers = userService.GetUsersByIds(tierList.Select(x => $"Discord|{x.Id}").ToList());
        tierUsers.ForEach(x => x.Permissions |= tier);
        userService.UpdateBulk(tierUsers);
        return tierUsers.Count;
    }

    private void UpdateUserLists(List<User> current, List<DiscordApiUser> incoming)
    {
        foreach (var discordApiUser in new List<DiscordApiUser>(incoming))
        {
            string id = "Discord|" + discordApiUser.Id;
            User? u = current.Find(x => x.Id == id);
            if (u != null)
            {
                incoming.Remove(discordApiUser);
                current.Remove(u);
            }
        }
    }
}