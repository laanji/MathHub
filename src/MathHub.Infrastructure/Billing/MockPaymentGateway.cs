using MathHub.Modules.Billing.Application;
namespace MathHub.Infrastructure.Billing;
public sealed class MockPaymentGateway : IPaymentGateway
{
    public Task<GatewayResult> ChargeAsync(Guid requestId, decimal amount, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); return Task.FromResult(new GatewayResult($"mock-{requestId:N}", amount > 0)); }
}
