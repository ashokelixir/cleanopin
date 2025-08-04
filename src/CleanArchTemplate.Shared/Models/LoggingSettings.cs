namespace CleanArchTemplate.Shared.Models;

public class LoggingSettings
{
    public const string SectionName = "Logging";
    
    public SerilogSettings Serilog { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
    public AuditSettings Audit { get; set; } = new();
}

public class SerilogSettings
{
    public string MinimumLevel { get; set; } = "Information";
    public Dictionary<string, string> Override { get; set; } = new();
    public List<SinkConfiguration> WriteTo { get; set; } = new();
    public List<string> Enrich { get; set; } = new();
    public PropertiesSettings Properties { get; set; } = new();
}

public class SinkConfiguration
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Args { get; set; } = new();
}

public class PropertiesSettings
{
    public string Application { get; set; } = "CleanArchTemplate";
    public string Environment { get; set; } = "Development";
    public string Version { get; set; } = "1.0.0";
}

public class PerformanceSettings
{
    public bool Enabled { get; set; } = true;
    public int SlowQueryThresholdMs { get; set; } = 1000;
    public int SlowRequestThresholdMs { get; set; } = 5000;
    public bool LogQueryParameters { get; set; } = false;
}

public class AuditSettings
{
    public bool Enabled { get; set; } = true;
    public List<string> AuditableEntities { get; set; } = new() { "User", "Role", "Permission" };
    public List<string> AuditableOperations { get; set; } = new() { "Create", "Update", "Delete" };
}