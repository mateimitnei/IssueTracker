using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IssueTracker.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Default: Server error
        var statusCode = StatusCodes.Status500InternalServerError;
        var title = "A server error occurred while processing the request.";
        
        // 400, 404, 409
        switch (exception)
        {
            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                title = "The requested resource could not be found.";
                break;
            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "One or more validation errors occurred.";
                break;
            case InvalidOperationException:
                statusCode = StatusCodes.Status409Conflict;
                title = "The request could not be completed due to a conflict.";
                break;
        }
        
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };
        
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        return true;
    }
}