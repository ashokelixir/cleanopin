using AutoMapper;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role)));
        
        CreateMap<User, UserSummaryDto>();

        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.Permission)));
        
        CreateMap<Role, RoleSummaryDto>();

        CreateMap<Permission, PermissionDto>();
        CreateMap<Permission, PermissionSummaryDto>();

        CreateMap<PermissionAuditLog, PermissionAuditLogDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => 
                src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : null))
            .ForMember(dest => dest.PermissionName, opt => opt.MapFrom(src => src.Permission != null ? src.Permission.Name : string.Empty))
            .ForMember(dest => dest.PermissionResource, opt => opt.MapFrom(src => src.Permission != null ? src.Permission.Resource : string.Empty))
            .ForMember(dest => dest.PermissionAction, opt => opt.MapFrom(src => src.Permission != null ? src.Permission.Action : string.Empty));
    }
}