using System.Text.Json;
using WeatherCli.Models;

namespace WeatherCli.Services.CredentialStorage;

public class FileCredentialStorage : ICredentialStorage
{
    private const string FilePath = ".credentials.txt";


    public void SaveToken(AuthToken token)
    {
        //In the future, this should be encypted using a machine provided local encryption or interfacing with a secure enclave.
        File.WriteAllText(FilePath, Encrypt(JsonSerializer.Serialize(token)));
    }

    public AuthToken? GetToken()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }


        var content = Decrypt(File.ReadAllText(FilePath));
        return content != null ? JsonSerializer.Deserialize<AuthToken>(content) : null;
    }

    public void ClearToken()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }

    /// <summary>
    /// Encrypts the data before saving it to the file.
    /// </summary>
    /// <param name="data">The data to encrypt</param>
    /// <returns>An encrypted string of the input data</returns>
    private string? Encrypt(string? data)
    {
        // In the future, data should be encypted using a machine provided local encryption or interfacing with a secure enclave.
        // For now, just return the data as is
        return data;
    }

    /// <summary>
    /// Decrypts the data read from the file.
    /// </summary>
    /// <param name="data">The data to decrypt</param>
    /// <returns>The decrypted string</returns>
    private string? Decrypt(string? data)
    {
        // In the future, data should be decypted using a machine provided local encryption or interfacing with a secure enclave.
        // For now, just return the data as is
        return data;
    }
}