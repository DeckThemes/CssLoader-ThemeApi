#region

using Microsoft.EntityFrameworkCore;

#endregion

namespace DeckPersonalisationApi.Model;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<SavedBlob> Blobs { get; set; }
    public DbSet<CssTheme> CssThemes { get; set; }
    public DbSet<CssSubmission> CssSubmissions { get; set; }
    public DbSet<MessageOfTheDay> MessageOfTheDay { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>();
        modelBuilder.Entity<SavedBlob>();
        modelBuilder.Entity<CssTheme>();
        modelBuilder.Entity<CssSubmission>();
    }
}