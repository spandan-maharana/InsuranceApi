using InsuranceApi.Services;

namespace InsuranceApi.Middleware;

/* Middleware -  It is a step, where every request passes through, before it reaches the controller. 
 * It can be used for logging, authentication, error handling, etc. 
 * In this case, we are using it for error handling.
*/

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); //We let the request pass through to the next middleware or controller.
        }
        catch (ConflictException ex) // Catching the ConflictException and returning a 409 Conflict status code with the error message.
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex) // Catching any other unhandled exceptions and returning a 500 Internal Server Error status code with a generic error message.
        {
            _logger.LogError(ex, "Unhandled error");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Something went wrong." });
        }
    }
}