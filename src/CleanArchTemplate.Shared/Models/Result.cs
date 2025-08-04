using CleanArchTemplate.Shared.Enums;

namespace CleanArchTemplate.Shared.Models;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful</param>
    /// <param name="status">The operation status</param>
    /// <param name="message">The result message</param>
    /// <param name="errors">The validation errors</param>
    protected Result(bool isSuccess, OperationStatus status, string? message = null, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Status = status;
        Message = message;
        Errors = errors?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the operation status
    /// </summary>
    public OperationStatus Status { get; }

    /// <summary>
    /// Gets the result message
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the validation errors
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets the HTTP status code for this result
    /// </summary>
    public int StatusCode => Status switch
    {
        OperationStatus.Success => 200,
        OperationStatus.ValidationError => 422,
        OperationStatus.NotFound => 404,
        OperationStatus.Unauthorized => 401,
        OperationStatus.Forbidden => 403,
        OperationStatus.BadRequest => 400,
        OperationStatus.Conflict => 409,
        OperationStatus.InternalError => 500,
        _ => 500
    };

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <param name="message">Optional success message</param>
    /// <returns>A successful result</returns>
    public static Result Success(string? message = null) =>
        new(true, OperationStatus.Success, message);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    /// <param name="status">The operation status</param>
    /// <param name="message">The error message</param>
    /// <param name="errors">The validation errors</param>
    /// <returns>A failed result</returns>
    public static Result Failure(OperationStatus status, string? message = null, IEnumerable<string>? errors = null) =>
        new(false, status, message, errors);

    /// <summary>
    /// Creates a validation error result
    /// </summary>
    /// <param name="errors">The validation errors</param>
    /// <param name="message">Optional error message</param>
    /// <returns>A validation error result</returns>
    public static Result ValidationError(IEnumerable<string> errors, string? message = null) =>
        new(false, OperationStatus.ValidationError, message, errors);

    /// <summary>
    /// Creates a not found result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A not found result</returns>
    public static Result NotFound(string? message = null) =>
        new(false, OperationStatus.NotFound, message);

    /// <summary>
    /// Creates an unauthorized result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>An unauthorized result</returns>
    public static Result Unauthorized(string? message = null) =>
        new(false, OperationStatus.Unauthorized, message);

    /// <summary>
    /// Creates a forbidden result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A forbidden result</returns>
    public static Result Forbidden(string? message = null) =>
        new(false, OperationStatus.Forbidden, message);

    /// <summary>
    /// Creates a bad request result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A bad request result</returns>
    public static Result BadRequest(string? message = null) =>
        new(false, OperationStatus.BadRequest, message);

    /// <summary>
    /// Creates a conflict result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A conflict result</returns>
    public static Result Conflict(string? message = null) =>
        new(false, OperationStatus.Conflict, message);

    /// <summary>
    /// Creates an internal error result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>An internal error result</returns>
    public static Result InternalError(string? message = null) =>
        new(false, OperationStatus.InternalError, message);
}