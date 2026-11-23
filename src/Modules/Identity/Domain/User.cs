using MathHub.SharedKernel.Domain;
namespace MathHub.Modules.Identity.Domain;
public enum UserRole { Student, Parent, Teacher, Administrator, Consultant }
public sealed class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public User(string email, string displayName, UserRole role, bool active)
    {
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email.Trim() || string.IsNullOrWhiteSpace(displayName))
            throw new BusinessRuleException("identity.invalid", "Некоректне ім’я або email.");
        Email = email.Trim().ToLowerInvariant(); DisplayName = displayName.Trim(); Role = role; IsActive = active;
    }
    public void Deactivate() => IsActive = false;
}
