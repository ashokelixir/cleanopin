using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Handler for UpdateTenantCommand
/// </summary>
public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantInfo>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;

    public UpdateTenantCommandHandler(
        ITenantService tenantService,
        ILogger<UpdateTenantCommandHandler> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the UpdateTenantCommand
    /// </summary>
    /// <param name="request">The command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant information</returns>
    public async Task<TenantInfo> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating tenant: {TenantId}", request.TenantId);

        var tenant = await _tenantService.UpdateTenantAsync(
            request.TenantId,
            request.Name,
            request.Configuration,
            cancellationToken);

        _logger.LogInformation("Tenant updated successfully: {TenantId} ({Name})", tenant.Id, tenant.Name);
        return tenant;
    }
}