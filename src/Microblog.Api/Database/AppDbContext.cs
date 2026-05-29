namespace Microblog.Api.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        // We only need this to pass the options to the base class
    }

    /*
        Run the following commands after each change
        1. dotnet ef migrations add InitialCreate -- InitialCreate is the name of the migration here, should change based on the changes made
        2. dotnet ef database update
    */

    public DbSet<Post> Posts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AuthToken> AuthTokens { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<UserFollow> UserFollows { get; set; }
    public DbSet<UserLike> UserLikes { get; set; }
    public DbSet<MediaFile> MediaFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}