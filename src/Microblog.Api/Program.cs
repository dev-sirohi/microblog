using Microblog.Api.Services.BackgroundProcesses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// AddOpenApi() is used to add OpenAPI/Swagger generation services - Swagger cannot function without this
builder.Services.AddOpenApi();
/* AddEndpointsApiExplorer() is used to create Swagger documents for minimal APIs (APIs created via MapGet)
   AddControllers() is used to create Swagger documents for controllers (APIs created via Controller classes) */
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = null; });
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                         throw new Exception("'DefaultConnection' not found"));
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    string redisConnectionString = builder.Configuration["Redis:ConnectionString"] ??
                                   throw new Exception("Could not fetch redis connection string");
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
    options.AddPolicy("CorsPolicy", builder =>
        builder.WithOrigins("http://localhost:3000")
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

// Custom Services - Remember: Once a service is dependent on another scoped service, it must also be registered as scoped
builder.Services.AddScoped<IRateLimiter, RateLimiter>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IUserFollowService, UserFollowService>();
builder.Services.AddScoped<IUserLikeService, UserLikeService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddHostedService<BackgroundSyncService>();

// Populating CacheConfigDict with Operation Types that are not neccessarily needed but background sync service requires to run
// Could have added a containskey check there but this way I won't have to add checks everywhere and there's no harm in adding all enums with default values - for now
Enum.GetValues<AppConstants.InMemoryOperationType>().ToList()
    .ForEach(e =>
    {
        if (!AppConstants.CacheConfigDict.ContainsKey(e))
            AppConstants.CacheConfigDict.Add(e, new AppConstants.CacheConfig());
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandlerMiddleware();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");
    app.UseTestMiddleware();
}
else
{
    app.UseCors("CorsPolicy");
}

app.UseHttpsRedirection();

var webRoot = app.Environment.WebRootPath
              ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadPath = Path.Combine(webRoot, "uploads");
Directory.CreateDirectory(uploadPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();