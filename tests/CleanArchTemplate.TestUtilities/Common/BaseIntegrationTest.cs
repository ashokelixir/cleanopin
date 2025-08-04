using CleanArchTemplate.Infrastructure.Data.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchTemplate.TestUtilities.Common;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly PostgreSqlContainer PostgreSqlContainer;
    protected readonly RedisContainer RedisContainer;
    protected readonly ITestOutputHelper Output;
    protected IServiceProvider ServiceProvider => Factory.Services;

    protected BaseIntegrationTest(ITestOutputHelper output)
    {
        Output = output;
        PostgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();

        RedisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add test database
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(PostgreSqlContainer.GetConnectionString());
                    });

                    // Configure Redis for testing - using in-memory cache for tests
                    services.AddMemoryCache();

                    // Reduce logging noise in tests
                    services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
                });
            });

        Client = Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await PostgreSqlContainer.StartAsync();
        await RedisContainer.StartAsync();

        // Ensure database is created and migrated
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await PostgreSqlContainer.DisposeAsync();
        await RedisContainer.DisposeAsync();
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(context);
    }

    protected async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(context);
    }

    protected async Task ResetDatabaseAsync()
    {
        await ExecuteDbContextAsync(async context =>
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        });
    }
}