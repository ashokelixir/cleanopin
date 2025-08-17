namespace CleanArchTemplate.Application.Common.Enums;

/// <summary>
/// Enumeration for bulk assignment operation types
/// </summary>
public enum BulkAssignmentType
{
    /// <summary>
    /// Assign permissions to a role
    /// </summary>
    Role = 1,

    /// <summary>
    /// Assign permissions to a user
    /// </summary>
    User = 2,

    /// <summary>
    /// Remove permissions from a role
    /// </summary>
    RemoveFromRole = 3,

    /// <summary>
    /// Remove permissions from a user
    /// </summary>
    RemoveFromUser = 4
}