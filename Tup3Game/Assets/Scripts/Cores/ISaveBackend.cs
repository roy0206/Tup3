using System.Threading.Tasks;

public interface ISaveBackend
{
    Task<string> LoadAsync(string key);
    Task SaveAsync(string key, string json);
}