using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Handler for CreateTenantCommand
/// </summary>
public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantInfo>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ITenantService tenantService,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the CreateTenantCommand
    /// </summary>
    /// <param name="request">The command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant information</returns>
    public async Task<TenantInfo> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating tenant: {Name} ({Identifier})", request.Name, request.Identifier);

        var tenant = await _tenantService.CreateTenantAsync(
            request.Name,
            request.Identifier,
            request.ConnectionString,
            request.Configuration,
            cancellationToken);

        // Set subscription expiry if provided
        if (request.SubscriptionExpiresAt.HasValue)
        {
            await _tenantService.SetSubscriptionExpiryAsync(tenant.Id, request.SubscriptionExpiresAt.Value, cancellationToken);
            tenant.SubscriptionExpiresAt = request.SubscriptionExpiresAt.Value;
        }

        _logger.LogInformation("Tenant created successfully: {TenantId} ({Name})", tenant.Id, tenant.Name);
        return tenant;
    }
}