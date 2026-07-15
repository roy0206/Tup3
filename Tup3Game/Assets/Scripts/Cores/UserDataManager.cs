using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class UserDataManager : Singleton<UserDataManager>
{
    const string SaveKey = "Data";
    static readonly JsonSerializerSettings Settings = new()
    {
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        Formatting = Formatting.Indented,
    };

    public UserData Data { get; private set; }

    ISaveBackend _local;
    

    protected override void OnAwake()
    {
        _local = new LocalSaveBackend();
    }

    public async Task LoadAsync()
    {
        string json = await _local.LoadAsync(SaveKey);

        Data = Deserialize(json);

        Debug.Log($"[UserDataManager] Loaded.");
    }

    public async Task SaveAsync()
    {
        try
        {
            string json = JsonConvert.SerializeObject(Data, Settings);
            Debug.Log($"[UserDataManager] Saving.");
            await _local.SaveAsync(SaveKey, json);

            Debug.Log("[UserDataManager] Saved.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserDataManager] 저장 실패. {e}");
        }
    }

    static UserData Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new UserData();

        try
        {
            return JsonConvert.DeserializeObject<UserData>(json, Settings) ?? new UserData();
        }
        catch (JsonException e)
        {
            Debug.LogError($"[UserDataManager] 세이브 파싱 실패 — 기본값으로 시작합니다. {e.Message}");
            return new UserData();
        }
    }
    
    public async Task ResetAsync()
    {
        Data = new UserData();
        await SaveAsync();
        Debug.Log("[UserDataManager] Reset.");
    }
}