namespace CleanArchTemplate.Domain.Enums;

/// <summary>
/// Represents the state of a user permission override
/// </summary>
public enum PermissionState
{
    /// <summary>
    /// Explicitly grants the permission to the user
    /// </summary>
    Grant = 1,

    /// <summary>
    /// Explicitly denies the permission to the user
    /// </summary>
    Deny = 2
}