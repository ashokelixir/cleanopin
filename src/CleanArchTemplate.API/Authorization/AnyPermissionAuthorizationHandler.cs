using Microsoft.AspNetCore.Authorization;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.API.Authorization;

/// <summary>
/// Authorization handler for any permission requirements
/// </summary>
public class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly IPermissionAuthorizationService _permissionAuthorizationService;
    private readonly ILogger<AnyPermissionAuthorizationHandler> _logger;

    public AnyPermissionAuthorizationHandler(
        IPermissionAuthorizationService permissionAuthorizationService,
        ILogger<AnyPermissionAuthorizationHandler> logger)
    {
        _permissionAuthorizationService = permissionAuthorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the authorization requirement
    /// </summary>
    /// <param name="context">The authorization handler context</param>
    /// <param name="requirement">The any permission requirement</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            var permissionsString = string.Join(", ", requirement.Permissions);
            _logger.LogWarning("Authorization failed: User is not authenticated for any of permissions {Permissions}", permissionsString);
            context.Fail();
            return;
        }

        try
        {
            var authorizationResult = await _permissionAuthorizationService.AuthorizeAnyAsync(
                context.User, 
                requirement.Permissions);

            if (authorizationResult.IsAuthorized)
            {
                var permissionsString = string.Join(", ", requirement.Permissions);
                _logger.LogDebug("Authorization successful: User has at least one of the permissions {Permissions}", permissionsString);
                context.Succeed(requirement);
            }
            else
            {
                var permissionsString = string.Join(", ", requirement.Permissions);
                _logger.LogInformation("Authorization failed: User does not have any of the permissions {Permissions}. Reason: {Reason}", 
                    permissionsString, authorizationResult.FailureReason);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            var permissionsString = string.Join(", ", requirement.Permissions);
            _logger.LogError(ex, "Error during authorization for any of permissions {Permissions}", permissionsString);
            context.Fail();
        }
    }
}