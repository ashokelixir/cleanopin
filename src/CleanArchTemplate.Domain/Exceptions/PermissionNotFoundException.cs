namespace CleanArchTemplate.Domain.Exceptions;

/// <summary>
/// Exception thrown when a permission is not found
/// </summary>
public class PermissionNotFoundException : DomainException
{
    public PermissionNotFoundException(string permissionName) 
        : base($"Permission '{permissionName}' was not found.")
    {
    }

    public PermissionNotFoundException(string permissionName, Exception innerException) 
        : base($"Permission '{permissionName}' was not found.", innerException)
    {
    }
}