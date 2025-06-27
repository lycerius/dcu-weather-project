using System.Text.Json;
using WeatherCli.Models;

namespace WeatherCli.Services.CredentialStorage;

public class FileCredentialStorage : ICredentialStorage
{
    private const string FilePath = ".credentials.txt";

    public void SaveToken(AuthToken token)
    {
        File.WriteAllText(FilePath, Encrypt(JsonSerializer.Serialize(token)));
    }

    public AuthToken? GetToken()
    {
        if (!File.Exists(FilePath))
            return null;
        return JsonSerializer.Deserialize<AuthToken>(Decrypt(File.ReadAllText(FilePath)));
    }

    public void ClearToken()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }

    /// <summary>
    /// Encrypts the data before saving it to the file.
    /// </summary>
    private string? Encrypt(string? data)
    {
        // In the future, data should be encrypted using a machine provided local encryption or interfacing with a secure enclave.
        // For now, just return the data as is
        return data;
    }

    /// <summary>
    /// Decrypts the data read from the file.
    /// </summary>
    private string? Decrypt(string? data)
    {
        // In the future, data should be decrypted using a machine provided local encryption or interfacing with a secure enclave.
        // For now, just return the data as is
        return data;
    }
}