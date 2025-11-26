namespace Microblog.Api.Database
{
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
            /* User Follow */
            modelBuilder.Entity<UserFollow>()
                .HasIndex(f => new { f.FollowerId, f.FollowingId })
                .IsUnique();
            modelBuilder.Entity<UserFollow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserFollow>()
                .HasOne(f => f.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserFollow>()
                .ToTable(t => t.HasCheckConstraint("CK_UserFollow_NoSelfFollow", "[FollowerId] <> [FollowingId]"));
            /* User Like */
            modelBuilder.Entity<UserLike>()
                .HasIndex(l => new { l.UserId, l.PostId })
                .IsUnique();
            modelBuilder.Entity<UserLike>()
                .HasOne(l => l.Post)
                .WithMany(p => p.UserLikes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserLike>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            /* Media Files */
            modelBuilder.Entity<MediaFile>().Ignore(m => m.User);
            modelBuilder.Entity<MediaFile>().Ignore(m => m.Post);
        }
    }
}
