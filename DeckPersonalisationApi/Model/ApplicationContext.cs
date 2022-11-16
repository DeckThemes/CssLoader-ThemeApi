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
    public DbSet<SavedImage> Images { get; set; }
    public DbSet<CssTheme> CssThemes { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>();
        modelBuilder.Entity<SavedImage>();
        modelBuilder.Entity<CssThemeImage>();
        modelBuilder.Entity<CssTheme>();
    }
}