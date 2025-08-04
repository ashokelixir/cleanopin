namespace CleanArchTemplate.Shared.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// Default pagination settings
    /// </summary>
    public static class Pagination
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int MinPageSize = 1;
    }

    /// <summary>
    /// Cache key prefixes and expiration times
    /// </summary>
    public static class Cache
    {
        public const string UserPrefix = "user:";
        public const string RolePrefix = "role:";
        public const string PermissionPrefix = "permission:";
        
        public static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan LongExpiration = TimeSpan.FromHours(2);
    }

    /// <summary>
    /// JWT token settings
    /// </summary>
    public static class Jwt
    {
        public const string Issuer = "CleanArchTemplate";
        public const string Audience = "CleanArchTemplate.API";
        public const int AccessTokenExpirationMinutes = 15;
        public const int RefreshTokenExpirationDays = 7;
    }

    /// <summary>
    /// HTTP header names
    /// </summary>
    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-ID";
        public const string RequestId = "X-Request-ID";
        public const string ApiVersion = "X-API-Version";
    }

    /// <summary>
    /// Default role names
    /// </summary>
    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string User = "User";
        public const string Moderator = "Moderator";
    }

    /// <summary>
    /// Permission names
    /// </summary>
    public static class Permissions
    {
        public const string ReadUsers = "users:read";
        public const string WriteUsers = "users:write";
        public const string DeleteUsers = "users:delete";
        public const string ReadRoles = "roles:read";
        public const string WriteRoles = "roles:write";
        public const string DeleteRoles = "roles:delete";
        public const string ManageSystem = "system:manage";
    }

    /// <summary>
    /// Resilience policy names
    /// </summary>
    public static class ResiliencePolicies
    {
        public const string Database = "database";
        public const string ExternalApi = "external-api";
        public const string Default = "default";
        public const string Critical = "critical";
        public const string NonCritical = "non-critical";
    }
}