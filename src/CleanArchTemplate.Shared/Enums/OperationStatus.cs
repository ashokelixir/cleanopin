namespace CleanArchTemplate.Shared.Enums;

/// <summary>
/// Represents the status of an operation
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// Operation completed successfully
    /// </summary>
    Success = 0,

    /// <summary>
    /// Operation failed due to validation errors
    /// </summary>
    ValidationError = 1,

    /// <summary>
    /// Operation failed due to not found resource
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// Operation failed due to unauthorized access
    /// </summary>
    Unauthorized = 3,

    /// <summary>
    /// Operation failed due to forbidden access
    /// </summary>
    Forbidden = 4,

    /// <summary>
    /// Operation failed due to bad request
    /// </summary>
    BadRequest = 5,

    /// <summary>
    /// Operation failed due to conflict
    /// </summary>
    Conflict = 6,

    /// <summary>
    /// Operation failed due to internal error
    /// </summary>
    InternalError = 7
}