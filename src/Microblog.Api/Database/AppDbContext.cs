namespace Microblog.Api.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AuthToken> AuthTokens { get; set; }
    public DbSet<UserFollow> UserFollows { get; set; }
    public DbSet<UserLike> UserLikes { get; set; }
}
