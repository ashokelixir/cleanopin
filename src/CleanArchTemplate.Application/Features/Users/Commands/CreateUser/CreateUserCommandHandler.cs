using AutoMapper;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordService _passwordService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        IPasswordService passwordService,
        IAuditLogService auditLogService,
        ILogger<CreateUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordService = passwordService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with email: {Email}", request.Email);

        // Create email value object first for validation
        if (!Email.TryCreate(request.Email, out var email))
        {
            _logger.LogWarning("Invalid email format provided: {Email}", request.Email);
            return Result<UserDto>.ValidationError(new[] { "Invalid email address format" });
        }

        // Check if user already exists
        if (await _unitOfWork.Users.IsEmailExistsAsync(email!, null, cancellationToken))
        {
            _logger.LogWarning("Attempt to create user with existing email: {Email}", request.Email);
            await _auditLogService.LogSecurityEventAsync(
                "DuplicateUserCreationAttempt",
                $"Attempt to create user with existing email: {request.Email}",
                additionalData: new { Email = request.Email });
            
            return Result<UserDto>.Conflict("User with this email already exists");
        }

        // Create user entity
        var user = User.Create(
            email!,
            request.FirstName,
            request.LastName,
            _passwordService.HashPassword(request.Password)
        );

        // Execute database operations within a resilient transaction
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Add user to repository
            await _unitOfWork.Users.AddAsync(user, cancellationToken);
        }, cancellationToken);

        // Log audit event (outside transaction to avoid issues if audit logging fails)
        await _auditLogService.LogUserActionAsync(
            "UserCreated",
            user.Id,
            $"User created with email: {user.Email.Value}",
            new { 
                Email = user.Email.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt
            });

        _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

        // Map to DTO and return
        var userDto = _mapper.Map<UserDto>(user);
        return Result<UserDto>.Success(userDto);
    }
}