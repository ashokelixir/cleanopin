using CleanArchTemplate.Application;
using CleanArchTemplate.Infrastructure;
using CleanArchTemplate.Infrastructure.Extensions;
using CleanArchTemplate.API.Configuration;
using CleanArchTemplate.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add AWS Secrets Manager to configuration pipeline
// This should be done early in the configuration process
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddSecretsManager(
        new[] { "database-credentials", "jwt-settings", "external-api-keys" },
        region: builder.Configuration["SecretsManager:Region"],
        environment: builder.Configuration["SecretsManager:Environment"],
        optional: true);
}

// Configure Serilog
builder.ConfigureSerilog();

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add security services
builder.Services.AddSecurityServices(builder.Configuration);

// Add rate limiting
builder.Services.AddRateLimiting(builder.Configuration);

// Add API versioning
builder.Services.AddCustomApiVersioning();

// Add controllers
builder.Services.AddControllers();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
var issuer = jwtSettings["Issuer"] ?? "CleanArchTemplate";
var audience = jwtSettings["Audience"] ?? "CleanArchTemplate";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add Swagger documentation
builder.Services.AddSwaggerDocumentation(builder.Configuration);

var app = builder.Build();

// Preload secrets and ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Preload secrets if configured
        await scope.ServiceProvider.PreloadSecretsAsync();
        
        // Ensure database is created and seeded
        await scope.ServiceProvider.EnsureDatabaseAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the application");
        throw;
    }
}

// Configure the HTTP request pipeline.
app.UseSwaggerDocumentation(app.Environment, builder.Configuration);

// Use correlation ID middleware (must be early in pipeline)
app.UseCorrelationId();

// Use Serilog request logging
app.ConfigureRequestLogging();

// Use custom request logging middleware for detailed logging
app.UseRequestLogging();

// Use security middleware
app.UseSecurityMiddleware(app.Environment);

// Use input validation middleware
app.UseInputValidation();

// Use rate limiting
app.UseRateLimitingMiddleware();

// Serve static files for Swagger UI customization
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Use API versioning
app.UseApiVersioning();

app.MapControllers();

try
{
    Log.Information("Starting CleanArchTemplate API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CleanArchTemplate API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make Program class accessible for testing
public partial class Program { }