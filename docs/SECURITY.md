# Security Implementation Guide

This document outlines the comprehensive security measures implemented in the Clean Architecture Template API.

## Overview

The API implements multiple layers of security including HTTPS enforcement, security headers, CORS policies, rate limiting, input validation, API versioning, and secure Swagger documentation.

## Security Features

### 1. HTTPS Enforcement and Security Headers

#### HTTPS Configuration
- **HTTPS Redirection**: All HTTP requests are automatically redirected to HTTPS
- **HSTS (HTTP Strict Transport Security)**: Enforces HTTPS for 365 days with subdomain inclusion
- **Status Code**: Uses 308 Permanent Redirect for HTTPS redirection

#### Security Headers
The following security headers are automatically added to all responses:

- **X-Content-Type-Options**: `nosniff` - Prevents MIME type sniffing
- **X-Frame-Options**: `DENY` - Prevents clickjacking attacks
- **X-XSS-Protection**: `1; mode=block` - Enables XSS filtering
- **Referrer-Policy**: `strict-origin-when-cross-origin` - Controls referrer information
- **Content-Security-Policy**: Environment-specific CSP policies
- **Permissions-Policy**: Restricts access to browser features
- **X-Permitted-Cross-Domain-Policies**: `none` - Prevents cross-domain policy files

### 2. CORS (Cross-Origin Resource Sharing)

#### Environment-Specific Configuration

**Development Environment:**
```json
{
  "AllowedOrigins": ["*"],
  "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH"],
  "AllowedHeaders": ["*"]
}
```

**Production Environment:**
```json
{
  "AllowedOrigins": ["https://yourdomain.com", "https://api.yourdomain.com"],
  "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
  "AllowedHeaders": ["Content-Type", "Authorization"]
}
```

### 3. Rate Limiting

#### Rate Limiting Policies

**Global Rate Limiting (IP-based):**
- Development: 1000 requests per minute
- Production: 60 requests per minute

**Authentication Endpoints:**
- Development: 50 requests per 15 minutes
- Production: 5 requests per 15 minutes

**User-Specific Endpoints:**
- Token bucket algorithm with 200 tokens (dev) / 100 tokens (prod)
- Replenishment: 50 tokens per minute (dev) / 25 tokens per minute (prod)

**General API Endpoints:**
- Sliding window: 10,000 requests per hour (dev) / 500 requests per hour (prod)

#### Rate Limiting Headers
When rate limits are exceeded, the API returns:
- **Status Code**: 429 Too Many Requests
- **Retry-After**: Time in seconds until next request is allowed
- **Response Body**: JSON with error message

### 4. Input Validation and Sanitization

#### Validation Patterns
The API automatically validates and sanitizes input for:

- **SQL Injection**: Detects common SQL injection patterns
- **XSS (Cross-Site Scripting)**: Identifies malicious script injection
- **Path Traversal**: Prevents directory traversal attacks
- **Content Length**: Limits input size to prevent DoS attacks

#### Validation Scope
- Query parameters
- Request headers (User-Agent, Referer, X-Forwarded-For)
- Request body (JSON and form data)

#### Excluded Paths
The following paths skip input validation:
- `/health` - Health check endpoints
- `/swagger` - Swagger documentation
- `/api-docs` - API documentation

### 5. API Versioning

#### Versioning Strategies
The API supports multiple versioning approaches:

- **URL Segment**: `/api/v1/users`
- **Query String**: `/api/users?version=1.0`
- **Header**: `X-Version: 1.0`
- **Media Type**: `Accept: application/json;ver=1.0`

#### Supported Versions
- **v1.0**: Current stable version
- **v2.0**: Future version (placeholder)

#### Default Behavior
- Default version: v1.0
- Version reporting enabled in response headers

### 6. Secure Swagger Documentation

#### Environment-Specific Access

**Development:**
- Swagger UI accessible without authentication
- Full API testing capabilities enabled
- Relaxed Content Security Policy for development tools

**Production:**
- Swagger UI requires basic authentication (configurable)
- API testing capabilities disabled for security
- Strict Content Security Policy
- Optional: Can be completely disabled in production

#### Authentication
Production Swagger access uses basic authentication:
- Default credentials: `swagger` / `swagger123!`
- **Note**: Change these credentials in production environments

#### Security Features
- Server information is not exposed in Swagger documentation
- Try-it-out functionality disabled in production
- Custom CSS injection for branding

## Configuration

### Environment Variables

#### Development (appsettings.Development.json)
```json
{
  "IsDevelopment": true,
  "Cors": {
    "AllowedOrigins": ["*"]
  },
  "RateLimit": {
    "GlobalPermitLimit": 1000,
    "AuthPermitLimit": 50
  },
  "Swagger": {
    "RequireAuthentication": false
  }
}
```

#### Production (appsettings.Production.json)
```json
{
  "IsDevelopment": false,
  "Cors": {
    "AllowedOrigins": ["https://yourdomain.com"]
  },
  "RateLimit": {
    "GlobalPermitLimit": 60,
    "AuthPermitLimit": 5
  },
  "Swagger": {
    "EnableInProduction": false,
    "RequireAuthentication": true
  }
}
```

## Implementation Details

### Middleware Pipeline Order
The security middleware is applied in the following order:

1. **Forwarded Headers** - Handle proxy headers
2. **HSTS** - HTTP Strict Transport Security (production only)
3. **HTTPS Redirection** - Force HTTPS
4. **Security Headers** - Add security headers
5. **CORS** - Cross-origin resource sharing
6. **Input Validation** - Validate and sanitize input
7. **Rate Limiting** - Apply rate limits
8. **Authentication** - JWT token validation
9. **Authorization** - Permission checks

### Rate Limiting Attributes

#### Controller-Level Rate Limiting
```csharp
[EnableRateLimiting("UserPolicy")]
public class UsersController : ControllerBase
{
    // All endpoints use UserPolicy rate limiting
}
```

#### Action-Level Rate Limiting
```csharp
[HttpPost("login")]
[EnableRateLimiting("AuthPolicy")]
public async Task<IActionResult> Login([FromBody] LoginCommand command)
{
    // This endpoint uses AuthPolicy rate limiting
}
```

### Custom Rate Limiting Attributes
```csharp
[AuthRateLimit] // Equivalent to [EnableRateLimiting("AuthPolicy")]
[UserRateLimit] // Equivalent to [EnableRateLimiting("UserPolicy")]
[ApiRateLimit]  // Equivalent to [EnableRateLimiting("ApiPolicy")]
```

## Security Best Practices

### 1. HTTPS Configuration
- Always use HTTPS in production
- Configure proper SSL certificates
- Enable HSTS with appropriate max-age
- Consider HSTS preloading for enhanced security

### 2. CORS Configuration
- Never use wildcard origins (`*`) in production
- Specify exact allowed origins
- Limit allowed methods to necessary ones only
- Avoid allowing credentials with wildcard origins

### 3. Rate Limiting
- Monitor rate limiting metrics
- Adjust limits based on usage patterns
- Implement different limits for different user tiers
- Consider implementing distributed rate limiting for scaled deployments

### 4. Input Validation
- Always validate input at the API boundary
- Use whitelist validation when possible
- Implement proper error handling for validation failures
- Log suspicious input patterns for security monitoring

### 5. API Versioning
- Plan version deprecation strategy
- Maintain backward compatibility when possible
- Document version changes clearly
- Implement proper version sunset notifications

### 6. Swagger Security
- Disable Swagger in production unless necessary
- Use proper authentication for production Swagger access
- Implement IP whitelisting for Swagger access if needed
- Regular security reviews of exposed API documentation

## Monitoring and Logging

### Security Events to Monitor
- Rate limiting violations
- Input validation failures
- Authentication failures
- Suspicious request patterns
- CORS violations

### Recommended Logging
```csharp
_logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
_logger.LogWarning("Malicious input detected: {Input}", sanitizedInput);
_logger.LogInformation("Authentication successful for user: {UserId}", userId);
```

## Compliance Considerations

### OWASP Top 10 Coverage
- **A01 - Broken Access Control**: JWT authentication and authorization
- **A02 - Cryptographic Failures**: HTTPS enforcement and secure headers
- **A03 - Injection**: Input validation and sanitization
- **A05 - Security Misconfiguration**: Secure default configurations
- **A06 - Vulnerable Components**: Regular dependency updates
- **A07 - Authentication Failures**: Rate limiting and secure authentication
- **A09 - Security Logging**: Comprehensive security event logging

### Additional Security Standards
- Implements security headers recommended by OWASP
- Follows Microsoft security guidelines for ASP.NET Core
- Adheres to REST API security best practices
- Implements defense-in-depth security strategy

## Troubleshooting

### Common Issues

#### Rate Limiting Not Working
- Verify rate limiting middleware is registered
- Check middleware order in pipeline
- Confirm rate limiting policies are configured
- Validate IP address extraction logic

#### CORS Errors
- Verify allowed origins configuration
- Check preflight request handling
- Confirm credentials configuration
- Validate request headers

#### Security Headers Missing
- Confirm security headers middleware is registered
- Check middleware order
- Verify environment-specific configuration
- Test with browser developer tools

#### Input Validation False Positives
- Review validation patterns
- Adjust validation rules if necessary
- Implement whitelist exceptions for legitimate use cases
- Monitor validation logs for patterns

## Updates and Maintenance

### Regular Security Tasks
- Review and update rate limiting thresholds
- Monitor security logs for anomalies
- Update security headers based on latest recommendations
- Review and update CORS policies
- Test security configurations regularly
- Update dependencies for security patches

### Security Auditing
- Perform regular security assessments
- Review access logs for suspicious patterns
- Test rate limiting effectiveness
- Validate input sanitization coverage
- Assess API versioning security implications