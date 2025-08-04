using System;
using System.Threading.Tasks;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service for executing operations with resilience patterns using Polly
/// </summary>
public interface IResilienceService
{
    /// <summary>
    /// Executes an async operation with the specified resilience policy
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="policyName">Name of the policy to apply</param>
    /// <returns>Result of the operation</returns>
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string policyName);

    /// <summary>
    /// Executes an async operation with the specified resilience policy
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="policyName">Name of the policy to apply</param>
    Task ExecuteAsync(Func<Task> operation, string policyName);

    /// <summary>
    /// Executes an async operation with the specified resilience policy and fallback
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="fallback">Fallback operation if primary operation fails</param>
    /// <param name="policyName">Name of the policy to apply</param>
    /// <returns>Result of the operation or fallback</returns>
    Task<T> ExecuteWithFallbackAsync<T>(Func<Task<T>> operation, Func<Task<T>> fallback, string policyName);

    /// <summary>
    /// Executes a synchronous operation with the specified resilience policy
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="policyName">Name of the policy to apply</param>
    /// <returns>Result of the operation</returns>
    T Execute<T>(Func<T> operation, string policyName);

    /// <summary>
    /// Executes a synchronous operation with the specified resilience policy
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="policyName">Name of the policy to apply</param>
    void Execute(Action operation, string policyName);
}