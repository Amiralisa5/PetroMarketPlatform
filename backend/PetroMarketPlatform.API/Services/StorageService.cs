using Microsoft.Extensions.Options;
using PetroMarketPlatform.API.Common;

namespace PetroMarketPlatform.API.Services;

/// <summary>
/// Object-storage abstraction (§8). Backed by the local filesystem in dev; in production an
/// S3-compatible impl (ArvanCloud / MinIO) implements the same interface so storage stays portable.
/// KYC documents must live in-country with least-privilege access (§7 data residency).
/// </summary>
public interface IStorageService
{
    Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken ct = default);
    Task<Stream?> OpenAsync(string objectKey, CancellationToken ct = default);
}

public class LocalStorageService : IStorageService
{
    private readonly string _root;

    public LocalStorageService(IOptions<StorageOptions> opts)
    {
        _root = Path.GetFullPath(opts.Value.LocalRoot);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken ct = default)
    {
        var safeFolder = string.Join("_", folder.Split(Path.GetInvalidFileNameChars()));
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var dir = Path.Combine(_root, safeFolder);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, safeName);
        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        return Path.Combine(safeFolder, safeName).Replace('\\', '/'); // object key
    }

    public Task<Stream?> OpenAsync(string objectKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, objectKey);
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }
}
