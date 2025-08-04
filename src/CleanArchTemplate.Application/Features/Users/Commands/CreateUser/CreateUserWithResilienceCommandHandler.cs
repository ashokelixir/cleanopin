using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Shared.Constants;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Example command handler demonstrating resilience patterns
/// This shows how to integrate resilience service into application layer operations
/// </summary>
public class CreateUserWithResilienceCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordService _passwordService;
    private readonly IResilienceService _resilienceService;
    private readonly ILogger<CreateUserWithResilienceCommandHandler> _logger;

    public CreateUserWithResilienceCommandHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        IPasswordService passwordService,
        IResilienceService resilienceService,
        ILogger<CreateUserWithResilienceCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordService = passwordService;
        _resilienceService = resilienceService;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _resilienceService.ExecuteAsync(
                async () => await CreateUserInternalAsync(request, cancellationToken),
                ApplicationConstants.ResiliencePolicies.Critical);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user with email: {Email}", request.Email);
            return Result<UserDto>.InternalError("Failed to create user due to system error");
        }
    }

    private async Task<Result<UserDto>> CreateUserInternalAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with resilience patterns: {Email}", request.Email);

        // Create email value object first for validation
        if (!Email.TryCreate(request.Email, out var email))
        {
            return Result<UserDto>.ValidationError(new[] { "Invalid email address format" });
        }

        // Check if user already exists with resilience
        var emailExists = await _resilienceService.ExecuteAsync(
            async () => await _unitOfWork.Users.IsEmailExistsAsync(email!, null, cancellationToken),
            ApplicationConstants.ResiliencePolicies.Database);

        if (emailExists)
        {
            return Result<UserDto>.Conflict("User with this email already exists");
        }

        // Hash password with resilience (in case of external password service)
        var hashedPassword = await _resilienceService.ExecuteAsync(
            async () => await Task.FromResult(_passwordService.HashPassword(request.Password)),
            ApplicationConstants.ResiliencePolicies.Default);

        // Create user entity
        var user = User.Create(
            email!,
            request.FirstName,
            request.LastName,
            hashedPassword
        );

        // Execute database operations with resilience and transaction using EF Core's execution strategy
        var result = await _resilienceService.ExecuteAsync(
            async () => await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Add user
                await _unitOfWork.Users.AddAsync(user, cancellationToken);
                
                _logger.LogInformation("Successfully created user: {UserId}", user.Id);
                return user;
            }, cancellationToken),
            ApplicationConstants.ResiliencePolicies.Critical);

        // Map to DTO with resilience (in case mapping involves external calls)
        var userDto = await _resilienceService.ExecuteAsync(
            async () => await Task.FromResult(_mapper.Map<UserDto>(result)),
            ApplicationConstants.ResiliencePolicies.Default);

        return Result<UserDto>.Success(userDto);
    }
}