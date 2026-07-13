using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class LocalSaveBackend : ISaveBackend
{
    string GetPath(string key) =>
        Path.Combine(Application.persistentDataPath, $"{key}.json");

    public Task<string> LoadAsync(string key)
    {
        string path = GetPath(key);
        if (!File.Exists(path))
            return Task.FromResult<string>(null);

        return Task.FromResult(File.ReadAllText(path));
    }

    public Task SaveAsync(string key, string json)
    {
        File.WriteAllText(GetPath(key), json);
        return Task.CompletedTask;
    }
}