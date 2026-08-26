using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Flags]
public enum BossFlag
{
    None  = 0,
    Soil = 1 << 0,
    Water = 1 << 1,
    Fire  = 1 << 2,
    Gold  = 1 << 3,
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
    public List<bool> skills = new(){false, false, false, false};

    public BossFlag clearedBosses = 0;

    public int willCoins = 4;

    public bool lobbyIntroDone = false;

    public string endingId = "";
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

/* [파일 노트]
 * PlayData.lobbyIntroDone : 로비 도입부(튜토리얼 복도) 대사를 이미 봤는지 여부. LobbyIntroDirector 가 읽고 쓴다.
 * PlayData 는 UserDataManager.ClearPlayData() 에서 통째로 새로 생성되므로,
 * "새 게임"에서는 자동으로 false 가 되어 도입부가 다시 재생되고 "이어하기"에서는 저장된 값이 유지된다.
 * 세이브 JSON 에 없던 필드라 기존 세이브를 불러오면 기본값 false 로 들어온다(= 도입부를 한 번 더 보게 됨).
 *
 * PlayData.willCoins : 의지 코인 보유량. 시작값 4 (새 게임 = ClearPlayData 로 새 PlayData 생성 시 적용).
 * 보스전 결과로만 움직이며 BossRoom 이 0 밑으로 내려가지 않게 clamp 한다.
 *   - 극(剋) 승리(= 스킬 획득) : -1
 *   - 생(生) 승리(= 승리 후 보스의 스킬을 받지 않고 거절) : +5
 *   - 패배 : -1
 * 세이브 JSON 에 필드가 없는 기존 세이브를 불러오면 기본값 4 로 들어온다.
 * 잔여 코인은 엔딩 분기에 사용될 예정(기획: 최종보스 재도전 / 코인 소진 시 배드엔딩).
 *
 * PlayData.endingId : 도달한 엔딩의 식별자("Ending1"~"Ending4"). 최종보스(추후 구현)가 결과에 따라 세팅하고
 * Ending 씬(Ending.cs)은 이 값을 판독만 한다. 임시로 EndingTrigger 가 willCoins 기반으로 세팅해 테스트한다.
 * PlayData 소속이므로 ClearPlayData() 로 새 게임을 시작하면 자동으로 "" 로 초기화된다.
 * 업적(AchievementData)은 별도 객체라 ClearPlayData 의 영향을 받지 않는다.
 */
