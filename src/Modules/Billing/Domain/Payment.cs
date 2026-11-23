namespace MathHub.Modules.Billing.Domain;
public enum PaymentStatus { Pending, Succeeded, Refunded }
public sealed record Payment(Guid Id, Guid EnrollmentId, decimal Amount, Guid RequestId, PaymentStatus Status);
