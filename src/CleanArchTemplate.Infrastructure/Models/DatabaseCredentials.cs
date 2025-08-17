using System.Text.Json.Serialization;

namespace CleanArchTemplate.Infrastructure.Models;

/// <summary>
/// Model for database credentials stored in AWS Secrets Manager
/// </summary>
public class DatabaseCredentials
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;
    
    [JsonPropertyName("port")]
    public int Port { get; set; } = 5432;
    
    [JsonPropertyName("dbname")]
    public string Database { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonPropertyName("engine")]
    public string Engine { get; set; } = "postgres";
    
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the clean host without port (if port is included in host field)
    /// </summary>
    public string CleanHost
    {
        get
        {
            if (string.IsNullOrEmpty(Host))
                return string.Empty;
                
            // If host contains port, extract just the hostname
            var colonIndex = Host.LastIndexOf(':');
            if (colonIndex > 0 && int.TryParse(Host.Substring(colonIndex + 1), out _))
            {
                return Host.Substring(0, colonIndex);
            }
            
            return Host;
        }
    }
}