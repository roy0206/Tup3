using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Flags]
public enum BossFlag
{
    None  = 0,
    Fire  = 1 << 0,
    Water = 1 << 1,
    Gold  = 1 << 2,
    Earth = 1 << 3,
}

public class UserData
{
    public AchievementData      Achievements { get; set; } = new();
    public SettingsData         Settings     { get; set; } = new();
    public PlayData          Play       { get; set; } = new();
}


public class PlayData
{
    public SerializedVector position;
    public float health;

    public BossFlag clearedBosses = 0;
}

public class AchievementData
{
    public Dictionary<string, bool> Achievements { get; set; } = new();

    public bool IsUnlocked(string achievementId) =>
        Achievements.TryGetValue(achievementId, out bool v) && v;

    public void Unlock(string achievementId) => Achievements[achievementId] = true;
}


public class SettingsData
{
    /*public string Language    { get; set; } = "KOREAN";*/

    float _musicVolume = 1f;
    float _sfxVolume   = 1f;

    public float MusicVolume
    {
        get => _musicVolume;
        set => _musicVolume = Mathf.Clamp01(value);
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Mathf.Clamp01(value);
    }
}
