using System.Text.Json;
using WeatherCli.Models;

namespace WeatherCli.Services.CredentialStorage;

public class FileCredentialStorage : ICredentialStorage
{
    private const string FilePath = ".credentials.txt";

    public void SaveToken(AuthToken token)
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(token));
    }

    public AuthToken? GetToken()
    {
        if (!File.Exists(FilePath))
            return null;

        var content = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<AuthToken>(content);
    }

    public void ClearToken()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
}