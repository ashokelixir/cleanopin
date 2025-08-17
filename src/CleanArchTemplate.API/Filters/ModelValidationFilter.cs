using CleanArchTemplate.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace CleanArchTemplate.API.Filters;

/// <summary>
/// Action filter that validates model state and returns standardized error responses
/// </summary>
public class ModelValidationFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var correlationId = context.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? 
                               context.HttpContext.TraceIdentifier;

            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            var errorResponse = new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Title = "Model Validation Error",
                Detail = "One or more model validation errors occurred.",
                Instance = correlationId,
                Errors = errors,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };

            context.Result = new BadRequestObjectResult(errorResponse);
        }

        base.OnActionExecuting(context);
    }
}

/// <summary>
/// Attribute for applying model validation to controllers or actions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateModelAttribute : ServiceFilterAttribute
{
    public ValidateModelAttribute() : base(typeof(ModelValidationFilter))
    {
    }
}