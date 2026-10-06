using System.Security.Cryptography;
using System.Text;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

/// <summary>
/// Encrypts sensitive database fields with AES-GCM. This protects values if only the database file is copied.
/// The separate local key file must be backed up with the database because encrypted values cannot be recovered without it.
/// </summary>
internal sealed class LocalDataEncryptor
{
    private const string Prefix = "ENC1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public LocalDataEncryptor(string keyPath)
    {
        _key = LoadOrCreateKey(keyPath);
    }

    public bool IsEncrypted(string value) => value.StartsWith(Prefix, StringComparison.Ordinal);

    public string Encrypt(string value)
    {
        if (IsEncrypted(value)) return value;

        var plainBytes = Encoding.UTF8.GetBytes(value);
        var cipherBytes = new byte[plainBytes.Length];
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];

        // AES-GCM both encrypts the value and creates a tag that detects tampering.
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var storedBytes = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, storedBytes, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, storedBytes, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, storedBytes, nonce.Length + tag.Length, cipherBytes.Length);
        return Prefix + Convert.ToBase64String(storedBytes);
    }

    public string Decrypt(string value)
    {
        // Plain values are accepted so an existing database can be migrated on its next startup.
        if (!IsEncrypted(value)) return value;

        var storedBytes = Convert.FromBase64String(value[Prefix.Length..]);
        if (storedBytes.Length < NonceSize + TagSize)
            throw new CryptographicException("The encrypted database value is incomplete.");

        var nonce = storedBytes[..NonceSize];
        var tag = storedBytes[NonceSize..(NonceSize + TagSize)];
        var cipherBytes = storedBytes[(NonceSize + TagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] LoadOrCreateKey(string keyPath)
    {
        if (File.Exists(keyPath)) return Convert.FromBase64String(File.ReadAllText(keyPath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(keyPath));
        if (directory is not null) Directory.CreateDirectory(directory);

        var key = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(keyPath, Convert.ToBase64String(key));

        // On macOS and Linux, restrict the key file to the current user where supported.
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return key;
    }
}
