using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// User service with resilience patterns for critical operations
/// </summary>
public class ResilientUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IResilienceService _resilienceService;
    private readonly ILogger<ResilientUserService> _logger;

    public ResilientUserService(
        IUserRepository userRepository,
        IResilienceService resilienceService,
        ILogger<ResilientUserService> logger)
    {
        _userRepository = userRepository;
        _resilienceService = resilienceService;
        _logger = logger;
    }

    /// <summary>
    /// Gets user by email with resilience patterns and fallback to cache
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>User or null if not found</returns>
    public async Task<User?> GetUserByEmailWithResilienceAsync(Email email)
    {
        return await _resilienceService.ExecuteWithFallbackAsync(
            // Primary operation - database lookup
            async () =>
            {
                _logger.LogDebug("Attempting to get user by email: {Email}", email.Value);
                return await _userRepository.GetByEmailAsync(email);
            },
            // Fallback operation - could be cache lookup or default behavior
            async () =>
            {
                _logger.LogWarning("Primary user lookup failed, using fallback for email: {Email}", email.Value);
                // In a real scenario, this could check a cache or return a default user
                return await Task.FromResult<User?>(null);
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }

    /// <summary>
    /// Creates a user with enhanced resilience for critical operation
    /// </summary>
    /// <param name="user">User to create</param>
    /// <returns>Created user</returns>
    public async Task<User> CreateUserWithResilienceAsync(User user)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogInformation("Creating user with resilience: {Email}", user.Email.Value);
                
                // Check if email already exists with resilience
                var emailExists = await _resilienceService.ExecuteAsync(
                    async () => await _userRepository.IsEmailExistsAsync(user.Email, null, CancellationToken.None),
                    ApplicationConstants.ResiliencePolicies.Database);

                if (emailExists)
                {
                    throw new InvalidOperationException($"User with email {user.Email.Value} already exists");
                }

                await _userRepository.AddAsync(user, CancellationToken.None);
                return user;
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }

    /// <summary>
    /// Gets all users with resilience and fallback to empty list
    /// </summary>
    /// <returns>List of users</returns>
    public async Task<IEnumerable<User>> GetAllUsersWithResilienceAsync()
    {
        return await _resilienceService.ExecuteWithFallbackAsync<IEnumerable<User>>(
            // Primary operation
            async () =>
            {
                _logger.LogDebug("Getting all users with resilience");
                return await _userRepository.GetAllAsync(CancellationToken.None);
            },
            // Fallback operation - return empty list
            async () =>
            {
                _logger.LogWarning("Failed to get users from database, returning empty list");
                return await Task.FromResult<IEnumerable<User>>(new List<User>());
            },
            ApplicationConstants.ResiliencePolicies.NonCritical);
    }

    /// <summary>
    /// Updates user with resilience patterns
    /// </summary>
    /// <param name="user">User to update</param>
    /// <returns>Task</returns>
    public async Task UpdateUserWithResilienceAsync(User user)
    {
        await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogInformation("Updating user with resilience: {UserId}", user.Id);
                
                // Verify user exists before updating
                var existingUser = await _resilienceService.ExecuteAsync(
                    async () => await _userRepository.GetByIdAsync(user.Id, CancellationToken.None),
                    ApplicationConstants.ResiliencePolicies.Database);

                if (existingUser == null)
                {
                    throw new InvalidOperationException($"User with ID {user.Id} not found");
                }

                await _userRepository.UpdateAsync(user, CancellationToken.None);
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }

    /// <summary>
    /// Deletes user with resilience patterns
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <returns>Task</returns>
    public async Task DeleteUserWithResilienceAsync(Guid userId)
    {
        await _resilienceService.ExecuteAsync(
            async () =>
            {
                _logger.LogInformation("Deleting user with resilience: {UserId}", userId);
                
                // Verify user exists before deleting
                var existingUser = await _resilienceService.ExecuteAsync(
                    async () => await _userRepository.GetByIdAsync(userId, CancellationToken.None),
                    ApplicationConstants.ResiliencePolicies.Database);

                if (existingUser == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent user: {UserId}", userId);
                    return;
                }

                await _userRepository.RemoveAsync(existingUser, CancellationToken.None);
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }

    /// <summary>
    /// Performs a health check on user repository with resilience
    /// </summary>
    /// <returns>True if healthy, false otherwise</returns>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            return await _resilienceService.ExecuteAsync(
                async () =>
                {
                    _logger.LogDebug("Performing user repository health check");
                    
                    // Simple query to test database connectivity
                    var users = await _userRepository.GetAllAsync(CancellationToken.None);
                    return true;
                },
                ApplicationConstants.ResiliencePolicies.NonCritical);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User repository health check failed");
            return false;
        }
    }
}