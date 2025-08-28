using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using MediatR;

namespace CleanArchTemplate.Application.Features.Tenants.Queries.GetTenant;

/// <summary>
/// Handler for GetTenantQuery
/// </summary>
public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantInfo?>
{
    private readonly ITenantService _tenantService;

    public GetTenantQueryHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Handles the GetTenantQuery
    /// </summary>
    /// <param name="request">The query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant information if found</returns>
    public async Task<TenantInfo?> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        return await _tenantService.GetTenantAsync(request.TenantId, cancellationToken);
    }
}