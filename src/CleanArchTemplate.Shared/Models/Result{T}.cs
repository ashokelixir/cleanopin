using CleanArchTemplate.Shared.Enums;

namespace CleanArchTemplate.Shared.Models;

/// <summary>
/// Represents the result of an operation with a value
/// </summary>
/// <typeparam name="T">The type of the result value</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful</param>
    /// <param name="status">The operation status</param>
    /// <param name="value">The result value</param>
    /// <param name="message">The result message</param>
    /// <param name="errors">The validation errors</param>
    private Result(bool isSuccess, OperationStatus status, T? value = default, string? message = null, IEnumerable<string>? errors = null)
        : base(isSuccess, status, message, errors)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the result value
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the result data (alias for Value)
    /// </summary>
    public T? Data => Value;

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    /// <param name="value">The result value</param>
    /// <param name="message">Optional success message</param>
    /// <returns>A successful result with a value</returns>
    public static Result<T> Success(T value, string? message = null) =>
        new(true, OperationStatus.Success, value, message);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="status">The operation status</param>
    /// <param name="message">The error message</param>
    /// <param name="errors">The validation errors</param>
    /// <returns>A failed result</returns>
    public new static Result<T> Failure(OperationStatus status, string? message = null, IEnumerable<string>? errors = null) =>
        new(false, status, default, message, errors);

    /// <summary>
    /// Creates a validation error result
    /// </summary>
    /// <param name="errors">The validation errors</param>
    /// <param name="message">Optional error message</param>
    /// <returns>A validation error result</returns>
    public new static Result<T> ValidationError(IEnumerable<string> errors, string? message = null) =>
        new(false, OperationStatus.ValidationError, default, message, errors);

    /// <summary>
    /// Creates a not found result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A not found result</returns>
    public new static Result<T> NotFound(string? message = null) =>
        new(false, OperationStatus.NotFound, default, message);

    /// <summary>
    /// Creates an unauthorized result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>An unauthorized result</returns>
    public new static Result<T> Unauthorized(string? message = null) =>
        new(false, OperationStatus.Unauthorized, default, message);

    /// <summary>
    /// Creates a forbidden result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A forbidden result</returns>
    public new static Result<T> Forbidden(string? message = null) =>
        new(false, OperationStatus.Forbidden, default, message);

    /// <summary>
    /// Creates a bad request result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A bad request result</returns>
    public new static Result<T> BadRequest(string? message = null) =>
        new(false, OperationStatus.BadRequest, default, message);

    /// <summary>
    /// Creates a conflict result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A conflict result</returns>
    public new static Result<T> Conflict(string? message = null) =>
        new(false, OperationStatus.Conflict, default, message);

    /// <summary>
    /// Creates an internal error result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>An internal error result</returns>
    public new static Result<T> InternalError(string? message = null) =>
        new(false, OperationStatus.InternalError, default, message);

    /// <summary>
    /// Implicitly converts a value to a successful result
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>A successful result with the value</returns>
    public static implicit operator Result<T>(T value) => Success(value);


}