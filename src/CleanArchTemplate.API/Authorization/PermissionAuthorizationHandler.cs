using Microsoft.AspNetCore.Authorization;
using CleanArchTemplate.Application.Common.Interfaces;

namespace CleanArchTemplate.API.Authorization;

/// <summary>
/// Authorization handler for permission requirements
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionAuthorizationService _permissionAuthorizationService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionAuthorizationService permissionAuthorizationService,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionAuthorizationService = permissionAuthorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the authorization requirement
    /// </summary>
    /// <param name="context">The authorization handler context</param>
    /// <param name="requirement">The permission requirement</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Authorization failed: User is not authenticated for permission {Permission}", requirement.Permission);
            context.Fail();
            return;
        }

        try
        {
            var authorizationResult = await _permissionAuthorizationService.AuthorizeAsync(
                context.User, 
                requirement.Permission);

            if (authorizationResult.IsAuthorized)
            {
                _logger.LogDebug("Authorization successful: User has permission {Permission}", requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation("Authorization failed: User does not have permission {Permission}. Reason: {Reason}", 
                    requirement.Permission, authorizationResult.FailureReason);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authorization for permission {Permission}", requirement.Permission);
            context.Fail();
        }
    }
}