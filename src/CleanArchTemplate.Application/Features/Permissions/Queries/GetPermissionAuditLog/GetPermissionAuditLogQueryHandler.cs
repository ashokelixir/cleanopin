using AutoMapper;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchTemplate.Application.Features.Permissions.Queries.GetPermissionAuditLog;

public class GetPermissionAuditLogQueryHandler : IRequestHandler<GetPermissionAuditLogQuery, PaginatedResult<PermissionAuditLogDto>>
{
    private readonly IPermissionAuditLogRepository _auditLogRepository;
    private readonly IMapper _mapper;

    public GetPermissionAuditLogQueryHandler(IPermissionAuditLogRepository auditLogRepository, IMapper mapper)
    {
        _auditLogRepository = auditLogRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<PermissionAuditLogDto>> Handle(GetPermissionAuditLogQuery request, CancellationToken cancellationToken)
    {
        // Get all audit logs
        var allAuditLogs = await _auditLogRepository.GetAllAsync(cancellationToken);
        
        // Apply filters
        var filteredLogs = allAuditLogs.AsEnumerable();

        if (request.UserId.HasValue)
        {
            filteredLogs = filteredLogs.Where(al => al.UserId == request.UserId.Value);
        }

        if (request.RoleId.HasValue)
        {
            filteredLogs = filteredLogs.Where(al => al.RoleId == request.RoleId.Value);
        }

        if (request.PermissionId.HasValue)
        {
            filteredLogs = filteredLogs.Where(al => al.PermissionId == request.PermissionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            filteredLogs = filteredLogs.Where(al => al.Action.Contains(request.Action));
        }

        if (!string.IsNullOrWhiteSpace(request.PerformedBy))
        {
            filteredLogs = filteredLogs.Where(al => al.PerformedBy.Contains(request.PerformedBy));
        }

        if (request.StartDate.HasValue)
        {
            filteredLogs = filteredLogs.Where(al => al.PerformedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            filteredLogs = filteredLogs.Where(al => al.PerformedAt <= request.EndDate.Value);
        }

        // Apply sorting
        filteredLogs = request.SortBy?.ToLower() switch
        {
            "action" => request.SortDirection?.ToLower() == "desc"
                ? filteredLogs.OrderByDescending(al => al.Action)
                : filteredLogs.OrderBy(al => al.Action),
            "performedby" => request.SortDirection?.ToLower() == "desc"
                ? filteredLogs.OrderByDescending(al => al.PerformedBy)
                : filteredLogs.OrderBy(al => al.PerformedBy),
            "userid" => request.SortDirection?.ToLower() == "desc"
                ? filteredLogs.OrderByDescending(al => al.UserId)
                : filteredLogs.OrderBy(al => al.UserId),
            "roleid" => request.SortDirection?.ToLower() == "desc"
                ? filteredLogs.OrderByDescending(al => al.RoleId)
                : filteredLogs.OrderBy(al => al.RoleId),
            _ => request.SortDirection?.ToLower() == "desc"
                ? filteredLogs.OrderByDescending(al => al.PerformedAt)
                : filteredLogs.OrderBy(al => al.PerformedAt)
        };

        var totalCount = filteredLogs.Count();

        var auditLogs = filteredLogs
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var auditLogDtos = _mapper.Map<List<PermissionAuditLogDto>>(auditLogs);

        return new PaginatedResult<PermissionAuditLogDto>(
            auditLogDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}