using CleanArchTemplate.API.Attributes;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchTemplate.API.Controllers;

/// <summary>
/// Controller for demonstrating messaging functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagingController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly SqsQueueManager _queueManager;
    private readonly ILogger<MessagingController> _logger;

    public MessagingController(
        IMessagePublisher messagePublisher,
        SqsQueueManager queueManager,
        ILogger<MessagingController> logger)
    {
        _messagePublisher = messagePublisher;
        _queueManager = queueManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates all configured queues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue creation results</returns>
    [HttpPost("queues/create")]
    [RequirePermission("API.Admin")]
    public async Task<IActionResult> CreateQueuesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var queueConfigurations = await _queueManager.CreateAllQueuesAsync(cancellationToken);
            
            _logger.LogInformation("Created {QueueCount} queues", queueConfigurations.Count);
            
            return Ok(new
            {
                Message = "Queues created successfully",
                Queues = queueConfigurations.Select(q => new
                {
                    Name = q.Key,
                    Url = q.Value.QueueUrl,
                    IsFifo = q.Value.IsFifo,
                    HasDeadLetterQueue = q.Value.DeadLetterQueue != null
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create queues");
            return StatusCode(500, new { Message = "Failed to create queues", Error = ex.Message });
        }
    }

    /// <summary>
    /// Publishes a test user created message
    /// </summary>
    /// <param name="request">The test message request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message publication result</returns>
    [HttpPost("test/user-created")]
    [RequirePermission("API.Admin")]
    public async Task<IActionResult> PublishTestUserCreatedMessageAsync(
        [FromBody] TestUserCreatedRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new UserCreatedMessage
            {
                UserId = request.UserId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsEmailVerified = request.IsEmailVerified,
                Roles = request.Roles,
                CorrelationId = HttpContext.TraceIdentifier
            };

            var messageId = await _messagePublisher.PublishAsync(message, "user-events", cancellationToken);
            
            _logger.LogInformation("Published test user created message with ID: {MessageId}", messageId);
            
            return Ok(new
            {
                Message = "Test user created message published successfully",
                MessageId = messageId,
                QueueName = "user-events"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish test user created message");
            return StatusCode(500, new { Message = "Failed to publish message", Error = ex.Message });
        }
    }

    /// <summary>
    /// Publishes a test permission assigned message
    /// </summary>
    /// <param name="request">The test message request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message publication result</returns>
    [HttpPost("test/permission-assigned")]
    [RequirePermission("API.Admin")]
    public async Task<IActionResult> PublishTestPermissionAssignedMessageAsync(
        [FromBody] TestPermissionAssignedRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new PermissionAssignedMessage
            {
                UserId = request.UserId,
                PermissionId = request.PermissionId,
                PermissionName = request.PermissionName,
                AssignedByUserId = request.AssignedByUserId,
                Reason = request.Reason,
                CorrelationId = HttpContext.TraceIdentifier
            };

            var messageId = await _messagePublisher.PublishAsync(message, "permission-events", cancellationToken);
            
            _logger.LogInformation("Published test permission assigned message with ID: {MessageId}", messageId);
            
            return Ok(new
            {
                Message = "Test permission assigned message published successfully",
                MessageId = messageId,
                QueueName = "permission-events"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish test permission assigned message");
            return StatusCode(500, new { Message = "Failed to publish message", Error = ex.Message });
        }
    }

    /// <summary>
    /// Publishes a batch of test messages
    /// </summary>
    /// <param name="request">The batch test request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch publication result</returns>
    [HttpPost("test/batch")]
    [RequirePermission("API.Admin")]
    public async Task<IActionResult> PublishTestBatchMessagesAsync(
        [FromBody] TestBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = Enumerable.Range(1, request.MessageCount)
                .Select(i => new UserCreatedMessage
                {
                    UserId = Guid.NewGuid(),
                    Email = $"test{i}@example.com",
                    FirstName = $"Test{i}",
                    LastName = "User",
                    IsEmailVerified = i % 2 == 0,
                    Roles = new List<string> { "User" },
                    CorrelationId = HttpContext.TraceIdentifier
                })
                .ToList();

            var messageIds = await _messagePublisher.PublishBatchAsync(messages, "user-events", cancellationToken);
            
            _logger.LogInformation("Published {MessageCount} test messages in batch", messageIds.Count());
            
            return Ok(new
            {
                Message = "Test batch messages published successfully",
                MessageIds = messageIds,
                QueueName = "user-events",
                MessageCount = messageIds.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish test batch messages");
            return StatusCode(500, new { Message = "Failed to publish batch messages", Error = ex.Message });
        }
    }

    /// <summary>
    /// Publishes a test FIFO message
    /// </summary>
    /// <param name="request">The FIFO test request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FIFO message publication result</returns>
    [HttpPost("test/fifo")]
    [RequirePermission("API.Admin")]
    public async Task<IActionResult> PublishTestFifoMessageAsync(
        [FromBody] TestFifoRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new UserCreatedMessage
            {
                UserId = request.UserId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsEmailVerified = true,
                Roles = new List<string> { "User" },
                CorrelationId = HttpContext.TraceIdentifier
            };

            var messageId = await _messagePublisher.PublishToFifoAsync(
                message, 
                "audit-events", 
                request.MessageGroupId, 
                request.DeduplicationId,
                cancellationToken);
            
            _logger.LogInformation("Published test FIFO message with ID: {MessageId}", messageId);
            
            return Ok(new
            {
                Message = "Test FIFO message published successfully",
                MessageId = messageId,
                QueueName = "audit-events",
                MessageGroupId = request.MessageGroupId,
                DeduplicationId = request.DeduplicationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish test FIFO message");
            return StatusCode(500, new { Message = "Failed to publish FIFO message", Error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for testing user created messages
/// </summary>
public class TestUserCreatedRequest
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "test@example.com";
    public string FirstName { get; set; } = "Test";
    public string LastName { get; set; } = "User";
    public bool IsEmailVerified { get; set; } = false;
    public List<string> Roles { get; set; } = new() { "User" };
}

/// <summary>
/// Request model for testing permission assigned messages
/// </summary>
public class TestPermissionAssignedRequest
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid PermissionId { get; set; } = Guid.NewGuid();
    public string PermissionName { get; set; } = "test.permission";
    public Guid AssignedByUserId { get; set; } = Guid.NewGuid();
    public string? Reason { get; set; } = "Testing purposes";
}

/// <summary>
/// Request model for testing batch messages
/// </summary>
public class TestBatchRequest
{
    public int MessageCount { get; set; } = 5;
}

/// <summary>
/// Request model for testing FIFO messages
/// </summary>
public class TestFifoRequest
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "fifo-test@example.com";
    public string FirstName { get; set; } = "FIFO";
    public string LastName { get; set; } = "Test";
    public string MessageGroupId { get; set; } = "test-group";
    public string? DeduplicationId { get; set; }
}
