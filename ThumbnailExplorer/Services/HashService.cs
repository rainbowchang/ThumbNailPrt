using System.Security.Cryptography;
using System.Text;

namespace ThumbnailExplorer.Services;

public class HashService
{
    private readonly ConfigurationService _configService;

    public HashService(ConfigurationService configService)
    {
        _configService = configService;
    }

    public string ComputeHash(string path, DateTime creationTime)
    {
        string input = path + creationTime.ToString("yyyyMMddHHmmss");
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string GetPropertyDirectoryPath(string hash)
    {
        string root = _configService.GetPropertyDirectoryRoot();
        return Path.Combine(root, hash);
    }

    public bool IsHashValid(string hash)
    {
        if (string.IsNullOrEmpty(hash) || hash.Length != 64)
            return false;

        return hash.All(c => char.IsLetterOrDigit(c));
    }
}
