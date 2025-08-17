using CleanArchTemplate.Application.Common.Behaviors;
using CleanArchTemplate.Application.Features.Users.Commands.CreateUser;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CleanArchTemplate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Add MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });

        // Override the default CreateUserCommandHandler with the resilient version
        services.AddScoped<IRequestHandler<CreateUserCommand, Result<UserDto>>, CreateUserWithResilienceCommandHandler>();

        // Add AutoMapper
        services.AddAutoMapper(assembly);

        // Add FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Add application services
        services.AddScoped<Services.IPermissionApplicationService, Services.PermissionApplicationService>();

        return services;
    }
}