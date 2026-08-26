using UnityEngine;

public static class VolumeSettings
{
    static bool dirty;

    static SettingsData Settings
    {
        get
        {
            var manager = UserDataManager.Instance;
            return manager != null ? manager.Data?.Settings : null;
        }
    }

    public static float Bgm
    {
        get
        {
            var settings = Settings;
            if (settings != null) return settings.MusicVolume;
            return AudioManager.Instance != null ? AudioManager.Instance.BGMVolume : 1f;
        }
    }

    public static float Sfx
    {
        get
        {
            var settings = Settings;
            if (settings != null) return settings.SfxVolume;
            return AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1f;
        }
    }

    public static void SetBgm(float value)
    {
        value = Mathf.Clamp01(value);

        var settings = Settings;
        if (settings != null)
        {
            settings.MusicVolume = value;
            dirty = true;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.BGMVolume = value;
    }

    public static void SetSfx(float value)
    {
        value = Mathf.Clamp01(value);

        var settings = Settings;
        if (settings != null)
        {
            settings.SfxVolume = value;
            dirty = true;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.SFXVolume = value;
    }

    public static void ApplySaved()
    {
        var settings = Settings;
        if (settings == null || AudioManager.Instance == null) return;

        AudioManager.Instance.BGMVolume = settings.MusicVolume;
        AudioManager.Instance.SFXVolume = settings.SfxVolume;
    }

    public static void SaveIfDirty()
    {
        if (!dirty) return;
        dirty = false;

        if (UserDataManager.Instance != null)
            _ = UserDataManager.Instance.SaveAsync();
    }
}

/* [파일 노트]
 *
 * SettingsData(UserData.Settings) ↔ AudioManager 믹서를 잇는 얇은 정적 브리지.
 * UserData.cs 의 기존 필드(MusicVolume/SfxVolume)만 읽고 쓰며, 필드 추가는 하지 않는다.
 *
 * - SetBgm/SetSfx : 옵션 슬라이더 콜백. SettingsData 갱신 + 믹서(BGM/SFX 파라미터) 즉시 반영.
 *   저장은 매 프레임 드래그마다 하지 않고 dirty 플래그만 세운다.
 * - SaveIfDirty  : 옵션 패널 닫힘/일시정지 해제 시 PauseManager 가 호출. UserDataManager.SaveAsync 로 저장.
 * - ApplySaved   : 세이브에서 읽어온 볼륨을 믹서에 적용. AudioManager.OnSceneLoadComplete 가 호출한다.
 *   (SceneController 는 항상 유저 데이터 로드를 끝낸 뒤 OnSceneLoadComplete 를 쏘므로
 *    게임 최초 부팅과 모든 씬 전환 시점에 저장값이 믹서에 반영된다.)
 * - 데이터가 아직 로드되지 않은 극초반에는 AudioManager 의 직렬화 기본값을 그대로 쓴다(null 안전).
 * - Master 볼륨은 UI 로 노출하지 않는다(기획: BGM/SFX 두 개만).
 */
