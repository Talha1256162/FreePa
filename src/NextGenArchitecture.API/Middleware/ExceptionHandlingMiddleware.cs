using System.Net;
using System.Text.Json;
using NextGenArchitecture.SharedKernel.Results;

namespace NextGenArchitecture.API.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and providing consistent error responses.
/// Implements comprehensive error handling with logging and security considerations.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <param name="environment">The host environment information.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing request {RequestPath}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles the exception and creates an appropriate HTTP response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = CreateErrorResponse(exception);
        response.StatusCode = errorResponse.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(errorResponse.Body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Creates an appropriate error response based on the exception type.
    /// </summary>
    /// <param name="exception">The exception to create a response for.</param>
    /// <returns>An error response with status code and body.</returns>
    private ErrorResponse CreateErrorResponse(Exception exception)
    {
        return exception switch
        {
            ArgumentException argEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = new
                {
                    error = "Bad Request",
                    message = argEx.Message,
                    details = _environment.IsDevelopment() ? argEx.StackTrace : null
                }
            },
            ArgumentNullException argNullEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = new
                {
                    error = "Bad Request",
                    message = $"Required parameter '{argNullEx.ParamName}' is missing.",
                    details = _environment.IsDevelopment() ? argNullEx.StackTrace : null
                }
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Body = new
                {
                    error = "Unauthorized",
                    message = "Access denied. Please authenticate and try again."
                }
            },
            KeyNotFoundException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Body = new
                {
                    error = "Not Found",
                    message = "The requested resource was not found."
                }
            },
            InvalidOperationException invalidOpEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = new
                {
                    error = "Invalid Operation",
                    message = invalidOpEx.Message,
                    details = _environment.IsDevelopment() ? invalidOpEx.StackTrace : null
                }
            },
            TimeoutException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Body = new
                {
                    error = "Request Timeout",
                    message = "The request timed out. Please try again later."
                }
            },
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = new
                {
                    error = "Internal Server Error",
                    message = _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An unexpected error occurred. Please try again later.",
                    details = _environment.IsDevelopment() ? exception.StackTrace : null
                }
            }
        };
    }

    /// <summary>
    /// Represents an error response with status code and body.
    /// </summary>
    private sealed class ErrorResponse
    {
        public int StatusCode { get; init; }
        public object Body { get; init; } = new();
    }
}