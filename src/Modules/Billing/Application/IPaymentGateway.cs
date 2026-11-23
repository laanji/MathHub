namespace MathHub.Modules.Billing.Application;
public sealed record GatewayResult(string Reference, bool Succeeded);
public interface IPaymentGateway
{
    Task<GatewayResult> ChargeAsync(Guid requestId, decimal amount, CancellationToken ct);
}
