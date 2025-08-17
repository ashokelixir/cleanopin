namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// DTO representing exported permission matrix data
/// </summary>
public class PermissionMatrixExportDto
{
    /// <summary>
    /// Export format (CSV, Excel, JSON)
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Exported data content
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Content type for the exported data
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Suggested filename for the export
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Export metadata
    /// </summary>
    public PermissionMatrixExportMetadataDto Metadata { get; set; } = new();

    /// <summary>
    /// Export timestamp
    /// </summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO representing import data for permission matrix
/// </summary>
public class PermissionMatrixImportDto
{
    /// <summary>
    /// Import format (CSV, Excel, JSON)
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Import data content
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Import options
    /// </summary>
    public PermissionMatrixImportOptionsDto Options { get; set; } = new();

    /// <summary>
    /// Import timestamp
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO representing import result for permission matrix
/// </summary>
public class PermissionMatrixImportResultDto
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Number of records processed
    /// </summary>
    public int ProcessedRecords { get; set; }

    /// <summary>
    /// Number of records successfully imported
    /// </summary>
    public int SuccessfulImports { get; set; }

    /// <summary>
    /// Number of records that failed to import
    /// </summary>
    public int FailedImports { get; set; }

    /// <summary>
    /// Number of records skipped
    /// </summary>
    public int SkippedRecords { get; set; }

    /// <summary>
    /// Import errors and warnings
    /// </summary>
    public IEnumerable<PermissionMatrixImportErrorDto> Errors { get; set; } = new List<PermissionMatrixImportErrorDto>();

    /// <summary>
    /// Import summary
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Import completion timestamp
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO representing export metadata
/// </summary>
public class PermissionMatrixExportMetadataDto
{
    /// <summary>
    /// Number of roles exported
    /// </summary>
    public int RoleCount { get; set; }

    /// <summary>
    /// Number of permissions exported
    /// </summary>
    public int PermissionCount { get; set; }

    /// <summary>
    /// Number of assignments exported
    /// </summary>
    public int AssignmentCount { get; set; }

    /// <summary>
    /// Export filters applied
    /// </summary>
    public Dictionary<string, string> Filters { get; set; } = new();

    /// <summary>
    /// Export options used
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// DTO representing import options
/// </summary>
public class PermissionMatrixImportOptionsDto
{
    /// <summary>
    /// Whether to validate data before import
    /// </summary>
    public bool ValidateData { get; set; } = true;

    /// <summary>
    /// Whether to skip invalid records
    /// </summary>
    public bool SkipInvalidRecords { get; set; } = true;

    /// <summary>
    /// Whether to create missing roles
    /// </summary>
    public bool CreateMissingRoles { get; set; } = false;

    /// <summary>
    /// Whether to create missing permissions
    /// </summary>
    public bool CreateMissingPermissions { get; set; } = false;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of errors before stopping import
    /// </summary>
    public int MaxErrors { get; set; } = 50;
}

/// <summary>
/// DTO representing an import error
/// </summary>
public class PermissionMatrixImportErrorDto
{
    /// <summary>
    /// Row number where the error occurred
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Error type (Validation, Processing, etc.)
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Field that caused the error
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Value that caused the error
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Whether this is a warning or error
    /// </summary>
    public bool IsWarning { get; set; }
}