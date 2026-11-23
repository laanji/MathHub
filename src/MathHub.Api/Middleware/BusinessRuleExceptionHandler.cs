using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MathHub.SharedKernel.Application;
using MathHub.SharedKernel.Domain;
namespace MathHub.Api.Middleware;
public sealed class BusinessRuleExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch { RequestRejectedException r => r.Kind switch { ErrorKind.Validation => 400, ErrorKind.Unauthorized => 401, ErrorKind.Forbidden => 403, ErrorKind.NotFound => 404, _ => 409 }, BadHttpRequestException r => r.StatusCode, BusinessRuleException => 422, _ => 500 };
        context.Response.StatusCode = status;
        var code = exception switch { RequestRejectedException r => r.Code, BusinessRuleException r => r.Code, _ => "server.error" };
        var details = new ProblemDetails { Status = status, Title = "Запит не виконано", Detail = status == 500 ? "Внутрішня помилка. Повторіть спробу або зверніться до адміністратора." : exception is BadHttpRequestException ? "Некоректні дані запиту. Перевірте заповнення полів." : exception.Message };
        details.Extensions["code"] = code; details.Extensions["traceId"] = context.TraceIdentifier;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details }); return true;
    }
}
