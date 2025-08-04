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
    }
}