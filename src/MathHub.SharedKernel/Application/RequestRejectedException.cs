namespace MathHub.SharedKernel.Application;
public enum ErrorKind { Validation, Unauthorized, Forbidden, NotFound, Conflict }
public sealed class RequestRejectedException(ErrorKind kind, string code, string message) : Exception(message)
{
    public ErrorKind Kind { get; } = kind;
    public string Code { get; } = code;
}
