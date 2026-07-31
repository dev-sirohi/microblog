using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace Microblog.Tests;

internal sealed class TestClientIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (ctx, nxt) =>
        {
            if (ctx.Request.Headers.TryGetValue("X-Test-Client-Ip", out var ip) &&
                IPAddress.TryParse(ip.ToString(), out var addr))
                ctx.Connection.RemoteIpAddress = addr;
            await nxt();
        });
        next(app);
    };
}

public sealed class MicroblogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sql.GetConnectionString(),
                ["Redis:ConnectionString"] = $"{_redis.GetConnectionString()},abortConnect=false,connectTimeout=15000,syncTimeout=15000,connectRetry=5,keepAlive=10",

                ["RateLimiting:Disabled"] = "false",
                ["Features:EnableEmbeddings"] = "false",
                ["Features:MessagingProvider"] = "none",
                ["Azure:KeyVaultUri"] = "",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
        });
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        await _redis.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sql.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}
