using Azure.Identity;
using Microblog.Api.Infrastructure.Caching;
using Microblog.Api.Infrastructure.Messaging;
using Microblog.Api.Infrastructure.Messaging.AzureServiceBus;
using Microblog.Api.Infrastructure.Messaging.Kafka;
using Microblog.Api.Infrastructure.Storage;
using Microblog.Api.Services.BackgroundProcesses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Keep this first so that KeyValut Config loads before use
var keyVaultUri = builder.Configuration["Azure:KeyVaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

builder.Services.AddSwaggerGen();

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

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (string.IsNullOrEmpty(ctx.Token) &&
                ctx.Request.Cookies.TryGetValue("accessToken", out var cookieToken))
            {
                ctx.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", b =>
        b.WithOrigins(allowedOrigins)
         .AllowCredentials().AllowAnyHeader().AllowAnyMethod());
});

// MVC
builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.PropertyNamingPolicy = null; });
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                         throw new Exception("'DefaultConnection' not found"));
});

// Infrastructure
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    string cs = builder.Configuration["Redis:ConnectionString"] ??
                throw new Exception("Redis:ConnectionString is required");
    return ConnectionMultiplexer.Connect(cs);
});
builder.Services.AddScoped<IRateLimiter, RateLimiter>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<GlobalConfig>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();

// Messaging
var messagingProvider = builder.Configuration["Features:MessagingProvider"];
if (!string.IsNullOrWhiteSpace(messagingProvider))
{
    switch (messagingProvider)
    {
        case "azure":
            builder.Services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
            break;
        case "kafka":
            builder.Services.AddSingleton<IMessagePublisher, KafkaProducer>();
            break;
    }
}

// Domain
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<UserFollowService>();
builder.Services.AddScoped<UserLikeService>();
builder.Services.AddHostedService<BackgroundSyncService>();

var app = builder.Build();

app.UseExceptionHandlerMiddleware();

app.UseCors("CorsPolicy");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
