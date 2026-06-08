using System.Threading.RateLimiting;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Microblog.Api.Features.Recommendations;
using Microblog.Api.Features.Recommendations.EmbeddingProviders;
using Microblog.Api.Infrastructure.Caching;
using Microblog.Api.Infrastructure.Messaging;
using Microblog.Api.Infrastructure.Messaging.AzureServiceBus;
using Microblog.Api.Infrastructure.Messaging.Kafka;
using Microblog.Api.Infrastructure.RateLimiting.Policies;
using Microblog.Api.Infrastructure.Storage;
using Microblog.Api.Interfaces.ProviderInterfaces;
using Microblog.Api.Services.BackgroundProcesses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Prometheus;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Azure Key Vault (load before anything else so secrets override appsettings) ───
var keyVaultUri = builder.Configuration["Azure:KeyVaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

// ─── Serilog ────────────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId());

// ─── Core MVC / Swagger ─────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = null; });
builder.Services.AddSwaggerGen();

// ─── Database ───────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                         throw new Exception("'DefaultConnection' not found"));
});

// ─── JWT Auth ───────────────────────────────────────────────────────────────────────
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

// ─── Redis ──────────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    string cs = builder.Configuration["Redis:ConnectionString"] ??
                throw new Exception("Redis:ConnectionString is required");
    return ConnectionMultiplexer.Connect(cs);
});

// RedLock for cache stampede protection
builder.Services.AddSingleton<IDistributedLockFactory>(sp =>
{
    var mux = sp.GetRequiredService<IConnectionMultiplexer>();
    return RedLockFactory.Create(new List<RedLockMultiplexer> { new RedLockMultiplexer(mux) });
});

// ─── CORS ───────────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    options.AddPolicy("CorsPolicy", b =>
        b.WithOrigins("http://localhost:3000")
         .AllowCredentials().AllowAnyHeader().AllowAnyMethod());
});

// ─── Rate Limiting (ASP.NET Core built-in + Redis-backed) ───────────────────────────
builder.Services.AddSingleton<AuthRateLimiterPolicy>();
builder.Services.AddSingleton<CreatePostRateLimiterPolicy>();
builder.Services.AddSingleton<FeedRateLimiterPolicy>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            Success = false,
            StatusCode = 429,
            Message = "Too many requests. Please try again later."
        }, ct);
    };

    if (builder.Environment.IsDevelopment())
    {
        // No limits in development
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            _ => RateLimitPartition.GetNoLimiter("dev"));
    }
    else
    {
        options.AddPolicy<string, AuthRateLimiterPolicy>(AuthRateLimiterPolicy.Name);
        options.AddPolicy<string, CreatePostRateLimiterPolicy>(CreatePostRateLimiterPolicy.Name);
        options.AddPolicy<string, FeedRateLimiterPolicy>(FeedRateLimiterPolicy.Name);
    }
});

// ─── Observability: OpenTelemetry ────────────────────────────────────────────────────
var serviceName = builder.Configuration["Observability:ServiceName"] ?? "microblog-api";
var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

// ─── Observability: Health Checks ────────────────────────────────────────────────────
var sqlConnection = builder.Configuration.GetConnectionString("DefaultConnection")!;
var redisConnection = builder.Configuration["Redis:ConnectionString"]!;

builder.Services.AddHealthChecks()
    .AddSqlServer(sqlConnection, name: "sqlserver", tags: ["ready", "db"])
    .AddRedis(redisConnection, name: "redis", tags: ["ready", "cache"]);

// ─── Resilience: Polly via IHttpClientFactory ────────────────────────────────────────
builder.Services.AddHttpClient("openai")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.MinimumThroughput = 8;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    });

// ─── Global config / misc ────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<GlobalConfig>();

// ─── Caching (cache-aside + RedLock) ─────────────────────────────────────────────────
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ─── Storage ─────────────────────────────────────────────────────────────────────────
var enableAzureStorage = builder.Configuration.GetValue<bool>("Features:EnableAzureStorage");
if (enableAzureStorage)
    builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
else
    builder.Services.AddScoped<IStorageService, LocalStorageService>();

// ─── Messaging ───────────────────────────────────────────────────────────────────────
var messagingProvider = builder.Configuration["Features:MessagingProvider"] ?? "none";
switch (messagingProvider.ToLowerInvariant())
{
    case "kafka":
        builder.Services.AddSingleton<IMessagePublisher, KafkaPublisher>();
        builder.Services.AddHostedService<KafkaConsumerService>();
        break;
    case "azure-service-bus":
        builder.Services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
        builder.Services.AddHostedService<ServiceBusConsumerService>();
        break;
}

// ─── Embeddings / Recommendations ────────────────────────────────────────────────────
var enableEmbeddings = builder.Configuration.GetValue<bool>("Features:EnableEmbeddings");
if (enableEmbeddings)
{
    builder.Services.AddScoped<IEmbeddingProvider, OpenAIEmbeddingProvider>();
    builder.Services.AddScoped<IRecommendationService, RecommendationService>();
}

// ─── Domain Services ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IUserFollowService, UserFollowService>();
builder.Services.AddScoped<IUserLikeService, UserLikeService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddHostedService<BackgroundSyncService>();

// Populate CacheConfigDict defaults for BackgroundSyncService
Enum.GetValues<AppConstants.InMemoryOperationType>().ToList()
    .ForEach(e =>
    {
        if (!AppConstants.CacheConfigDict.ContainsKey(e))
            AppConstants.CacheConfigDict.Add(e, new AppConstants.CacheConfig());
    });

// ─── Build ───────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Pipeline ────────────────────────────────────────────────────────────────────────
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

// Prometheus scrape endpoint at /metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.UseHttpsRedirection();
app.UseRateLimiter();

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

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
