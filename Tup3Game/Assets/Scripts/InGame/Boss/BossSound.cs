using System.Collections.Generic;
using UnityEngine;

public static class BossSound
{
    private static readonly Dictionary<string, float> lastPlayTimes = new();

    public static int Play(string soundName, float volume = 1f)
    {
        if (string.IsNullOrWhiteSpace(soundName)) return -1;

        AudioManager audio = AudioManager.Instance;
        if (audio == null) return -1;

        return audio.PlaySound(soundName, Mathf.Clamp01(volume));
    }

    public static int PlayThrottled(string soundName, float volume, float minInterval)
    {
        if (string.IsNullOrWhiteSpace(soundName)) return -1;

        if (minInterval > 0f)
        {
            float now = Time.unscaledTime;
            if (lastPlayTimes.TryGetValue(soundName, out float last) && now - last < minInterval) return -1;
            lastPlayTimes[soundName] = now;
        }

        return Play(soundName, volume);
    }

    public static int PlayLoop(string soundName, float volume = 1f)
    {
        if (string.IsNullOrWhiteSpace(soundName)) return -1;

        AudioManager audio = AudioManager.Instance;
        if (audio == null) return -1;

        return audio.PlayLoopingSound(soundName, Mathf.Clamp01(volume));
    }

    public static void Stop(int id)
    {
        if (id < 0) return;

        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;

        audio.StopSound(id);
    }

    public static string PickVariant(string first, string second)
    {
        if (string.IsNullOrWhiteSpace(first)) return second;
        if (string.IsNullOrWhiteSpace(second)) return first;
        return Random.value < 0.5f ? first : second;
    }
}

/* [파일 노트]
 * 보스 효과음 재생 창구. Assets/Scripts/InGame/Boss/** 의 모든 재생 호출이 이 정적 클래스를 거친다.
 * 직접 AudioManager.Instance.PlaySound 를 부르지 않고 한 겹 두른 이유는 세 가지다.
 *   1) 이름이 비어 있으면(= 그 보스에 배정된 파일이 없으면) 아무것도 하지 않는다.
 *      AudioManager 는 없는 클립 이름에 대해 LogWarning 을 남기므로, 미배정 필드를 그냥 두면
 *      전투 내내 경고가 쏟아진다. IsNullOrWhiteSpace 게이트가 그것을 막는다.
 *   2) PlayThrottled 로 "같은 이름의 소리는 minInterval 안에 다시 울리지 않는다"를 한곳에서 처리한다.
 *      한 프레임에 동시에 여러 개가 생성되는 소환물(수보스 물기둥 3개·고드름 3개, 최종보스 화염구 8개)과
 *      매 프레임 호출될 수 있는 지점(발소리·피격)이 겹쳐 울려 시끄러워지는 것을 막는다.
 *      기준 시각은 Time.unscaledTime 이라 일시정지(timeScale 0)에도 간격 계산이 정상이다.
 *   3) 반환 id 를 다루는 정지(Stop)와 랜덤 변형 선택(PickVariant)을 공통화한다.
 * 널 가드 관례는 Interaction/InteractionBase.cs 와 같다(AudioManager 는 Singleton<T> 이라 Instance 가
 * 널을 돌려주지 않지만, 형태상 방어 검사는 남겨 둔다).
 * 사운드 이름 문자열은 이 파일이 아니라 각 보스 스크립트가 const / SerializeField 로 들고 있다.
 */
