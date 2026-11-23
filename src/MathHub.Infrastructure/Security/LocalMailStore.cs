using System.Text.Json;
namespace MathHub.Infrastructure.Security;
// Development delivery only. Never sends messages to real recipients.
public sealed class LocalMailStore(string directory)
{
    public async Task DeliverAsync(Guid childId, string recipient, string code, CancellationToken ct)
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, $"{childId}.json"), JsonSerializer.Serialize(new { recipient, subject = "MathHub: підтвердження батьків", code, childId }), ct);
    }
    public async Task<string?> ReadAsync(Guid childId, CancellationToken ct)
    {
        var path = Path.Combine(directory, $"{childId}.json"); return File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : null;
    }
}
