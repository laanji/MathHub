using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MathHub.SharedKernel.Domain;
namespace MathHub.Api.Middleware;
public sealed class BusinessRuleExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException rule) return false;
        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        var details = new ProblemDetails { Status = 422, Title = "Не виконано правило предметної області", Detail = rule.Message };
        details.Extensions["code"] = rule.Code; details.Extensions["traceId"] = context.TraceIdentifier;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
        return true;
    }
}
