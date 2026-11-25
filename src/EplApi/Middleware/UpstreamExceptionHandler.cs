using System.Net;
using Microsoft.AspNetCore.Diagnostics;

namespace EplApi.Middleware;

// Turns a failed call to football-data.org into 503 or 502 for every endpoint.
internal sealed class UpstreamExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<UpstreamExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not HttpRequestException upstream)
            return false; // Not ours: the default handler returns 500.

        var (status, title, detail) =
            upstream.StatusCode == HttpStatusCode.TooManyRequests
                ? (
                    StatusCodes.Status503ServiceUnavailable,
                    "Football data is busy",
                    "The call limit of football-data.org is reached. Try again in one minute."
                )
                : (
                    StatusCodes.Status502BadGateway,
                    "Football data failed",
                    "football-data.org did not return the table."
                );

        logger.LogWarning(
            exception,
            "football-data.org failed with {StatusCode}.",
            upstream.StatusCode
        );

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails =
                {
                    Status = status,
                    Title = title,
                    Detail = detail,
                },
            }
        );
    }
}
