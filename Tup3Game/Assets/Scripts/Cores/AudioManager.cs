using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

// 오디오 매니저. SFX 채널 풀 + BGM/앰비언스 레이어(이름별, 중첩). 볼륨은 AudioMixer 그룹(Master/SFX/BGM)으로 제어한다.
// 사용법:
//   AudioManager.Instance.PlaySound("Hit", 1f, 1);             // SFX 1회 재생, id 반환
//   AudioManager.Instance.StopSound(id);                       // 재생 중 SFX 중단/반환
//   AudioManager.Instance.PlayBGM("MenuTheme");                // BGM 루프(이름별 레이어, 중첩 가능)
//   AudioManager.Instance.PlayBGM("Rain01");                   // 위와 동시에 깔림(전역 앰비언스)
//   AudioManager.Instance.StopBGM("Rain01");                   // 특정 레이어만 정지
//   AudioManager.Instance.MasterVolume = 0.5f;                 // 믹서에 즉시 반영(라이브)
// 에디터 셋업:
//   - AudioMixer 에셋(Master > SFX, BGM 그룹)을 만들고 각 그룹 Volume 을 스크립트로 노출.
//   - 노출 파라미터 이름은 아래 상수(MasterParam/SfxParam/BgmParam)와 정확히 일치해야 한다.
//   - 인스펙터에 mixer / sfxGroup / bgmGroup 지정. (SFX 채널은 코드에서 생성하므로 프리팹 불필요)
public class AudioManager : Singleton<AudioManager>, ISceneEventListener
{
    [Header("Mixer")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioMixerGroup sfxGroup;
    [SerializeField] AudioMixerGroup bgmGroup;

    // 믹서 에셋에서 동일한 이름으로 노출해야 하는 파라미터 키
    const string MasterParam = "Master";
    const string SfxParam = "SFX";
    const string BgmParam = "BGM";

    [Header("Addressables")]
    [Tooltip("Preloads every AudioClip with this Addressables label.")]
    [SerializeField] string preloadLabel = "Audio";

    [Header("Volume (0..1)")]
    [SerializeField, Range(0f, 1f)] float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] float bgmVolume = 1f;

    // set 시 믹서에 dB 로 즉시 반영 → 재생 중인 SFX/BGM 전부 라이브 적용된다.
    public float MasterVolume
    {
        get => masterVolume;
        set { masterVolume = Mathf.Clamp01(value); ApplyVolume(MasterParam, masterVolume); }
    }
    public float SFXVolume
    {
        get => sfxVolume;
        set { sfxVolume = Mathf.Clamp01(value); ApplyVolume(SfxParam, sfxVolume); }
    }
    public float BGMVolume
    {
        get => bgmVolume;
        set { bgmVolume = Mathf.Clamp01(value); ApplyVolume(BgmParam, bgmVolume); }
    }

    // 재생 중 채널 상태. remaining = 남은 반복 횟수.
    struct ChannelState
    {
        public AudioSource source;
        public int remaining;
    }

    readonly Dictionary<string, AudioClip> _clips = new();
    readonly Dictionary<int, ChannelState> _active = new();
    readonly List<int> _toRemove = new();
    readonly List<int> _toReplay = new();

    readonly Dictionary<string, AudioSource> _bgmLayers = new();

    Transform _poolParent;
    int _nextId = 1;
    AsyncOperationHandle<IList<AudioClip>> _soundLoadHandle;
    bool _soundLoadStarted;

    public bool SoundsReady { get; private set; }

    protected override void OnAwake()
    {
        _poolParent = transform;

        StartCoroutine(LoadAllSounds());

        // 직렬화된 초기 볼륨을 믹서에 반영
        ApplyVolume(MasterParam, masterVolume);
        ApplyVolume(SfxParam, sfxVolume);
        ApplyVolume(BgmParam, bgmVolume);
        
        SceneController.Instance.RegisterListener(this);
    }

    void Update()
    {
        if (_active.Count == 0) return;

        _toRemove.Clear();
        _toReplay.Clear();

        foreach (var kv in _active)
        {
            var st = kv.Value;
            if (st.source == null) { _toRemove.Add(kv.Key); continue; }
            if (st.source.isPlaying) continue;

            if (st.remaining > 1) _toReplay.Add(kv.Key);
            else _toRemove.Add(kv.Key);
        }

        for (int i = 0; i < _toReplay.Count; i++)
        {
            int id = _toReplay[i];
            var st = _active[id];
            st.remaining--;
            _active[id] = st;
            st.source.Play();
        }

        for (int i = 0; i < _toRemove.Count; i++)
            ReturnChannel(_toRemove[i]);
    }
    
    public int PlaySound(string soundName, float volume = 1f, int repeatTime = 1)
    {
        return PlaySoundInternal(soundName, volume, repeatTime, false);
    }

    public int PlayLoopingSound(string soundName, float volume = 1f)
    {
        return PlaySoundInternal(soundName, volume, 1, true);
    }

    public int PlayGlobalSound(string soundName, float volume = 1f, int repeatTime = 1)
    {
        return PlaySoundInternal(soundName, volume, repeatTime, false);
    }

    public void SetSoundVolume(int id, float volume)
    {
        if (!_active.TryGetValue(id, out var st) || st.source == null) return;
        st.source.volume = Mathf.Clamp01(volume);
    }

    public bool IsSoundPlaying(int id)
    {
        return _active.TryGetValue(id, out var st) && st.source != null && st.source.isPlaying;
    }

    int PlaySoundInternal(string soundName, float volume, int repeatTime, bool loop)
    {
        if (!_clips.TryGetValue(soundName, out var clip))
        {
            WarnUnavailableSound(soundName);
            return -1;
        }

        var ch = GetChannel();
        if (ch == null) return -1;

        ch.transform.SetParent(_poolParent, false);
        ch.transform.localPosition = Vector3.zero;
        ch.clip = clip;
        ch.volume = Mathf.Clamp01(volume);
        ch.loop = loop;
        ch.spatialBlend = 0f;
        ch.Play();

        int id = _nextId++;
        _active[id] = new ChannelState { source = ch, remaining = loop ? int.MaxValue : Mathf.Max(1, repeatTime) };
        return id;
    }

    public void StopSound(int id) => ReturnChannel(id);

    AudioSource GetChannel()
    {
        // 풀(자식)에서 비활성 채널 재사용
        for (int i = 0; i < _poolParent.childCount; i++)
        {
            var c = _poolParent.GetChild(i).GetComponent<AudioSource>();
            if (c != null && !c.gameObject.activeSelf)
            {
                c.gameObject.SetActive(true);
                return c;
            }
        }

        return CreateChannel();
    }
    
    AudioSource CreateChannel()
    {
        var go = new GameObject("SFXChannel");
        go.transform.SetParent(_poolParent, false);

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.outputAudioMixerGroup = sfxGroup;
        src.spatialBlend = 0f;
        return src;
    }

    void ReturnChannel(int id)
    {
        if (!_active.TryGetValue(id, out var st)) return;

        if (st.source != null)
        {
            st.source.Stop();
            st.source.clip = null;
            st.source.loop = false;
            st.source.transform.SetParent(_poolParent, false);
            st.source.gameObject.SetActive(false);
        }
        _active.Remove(id);
    }
    
    public void PlayBGM(string soundName, float volume = 1f, bool loop = true)
    {
        if (!_clips.TryGetValue(soundName, out var clip))
        {
            WarnUnavailableSound(soundName);
            return;
        }

        var src = GetOrCreateBgmLayer(soundName);
        src.loop = loop;
        src.volume = Mathf.Clamp01(volume);

        if (src.clip == clip && src.isPlaying) return; // 이미 재생 중이면 볼륨만 갱신

        src.clip = clip;
        src.Play();
    }
    
    public void StopBGM(string soundName)
    {
        if (_bgmLayers.TryGetValue(soundName, out var src) && src != null) src.Stop();
    }

    public void StopALlBGM()
    {
        foreach (var src in _bgmLayers.Values)
            if (src != null) src.Stop();
    }

    AudioSource GetOrCreateBgmLayer(string soundName)
    {
        if (_bgmLayers.TryGetValue(soundName, out var src) && src != null) return src;

        var go = new GameObject($"BGM_{soundName}");
        go.transform.SetParent(_poolParent, false);

        src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f; // 2D 전역
        src.outputAudioMixerGroup = bgmGroup;

        _bgmLayers[soundName] = src;
        return src;
    }

    void ApplyVolume(string param, float linear01)
    {
        if (mixer == null) return; 
        float dB = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        mixer.SetFloat(param, dB);
    }

    IEnumerator LoadAllSounds()
    {
        if (_soundLoadStarted)
            yield break;

        _soundLoadStarted = true;
        SoundsReady = false;
        _clips.Clear();

        if (string.IsNullOrWhiteSpace(preloadLabel))
        {
            Debug.LogError("[AudioManager] Addressables preload label is empty.");
            yield break;
        }

        _soundLoadHandle = Addressables.LoadAssetsAsync<AudioClip>(preloadLabel, null);
        yield return _soundLoadHandle;

        if (_soundLoadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[AudioManager] Failed to preload sounds with Addressables label '{preloadLabel}'. " +
                           "Make sure the AudioClips are Addressable and have that label.");
            yield break;
        }

        foreach (var clip in _soundLoadHandle.Result)
        {
            if (clip == null)
                continue;

            if (_clips.ContainsKey(clip.name))
            {
                Debug.LogWarning($"[AudioManager] Duplicate AudioClip name '{clip.name}'. " +
                                 "Sound names must be unique; the last loaded clip will be used.");
            }

            _clips[clip.name] = clip;
        }

        SoundsReady = true;
        Debug.Log($"[AudioManager] Preloaded {_clips.Count} sounds from Addressables label '{preloadLabel}'.");
    }

    void WarnUnavailableSound(string soundName)
    {
        if (!SoundsReady)
        {
            Debug.LogWarning($"[AudioManager] Sound '{soundName}' was requested before Addressables preload completed.");
            return;
        }

        Debug.LogWarning($"[AudioManager] Sound '{soundName}' was not found in Addressables label '{preloadLabel}'.");
    }

    void OnDestroy()
    {
        if (_soundLoadHandle.IsValid())
            Addressables.Release(_soundLoadHandle);

        _clips.Clear();
        SoundsReady = false;
    }

    public void OnSceneLoadComplete(string sceneName) => VolumeSettings.ApplySaved();
    public void OnSceneExit(string sceneName) => DisableAllChannels();

    void DisableAllChannels()
    {
        foreach (var kv in _active)
        {
            var st = kv.Value;
            if (st.source == null) continue;
            st.source.Stop();
            st.source.transform.SetParent(_poolParent, false);
            st.source.gameObject.SetActive(false);
        }
        _active.Clear();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        ApplyVolume(MasterParam, masterVolume);
        ApplyVolume(SfxParam, sfxVolume);
        ApplyVolume(BgmParam, bgmVolume);
    }
#endif
}

/* [파일 노트]
 * OnSceneLoadComplete : SceneController 가 유저 데이터 로드를 마친 뒤 호출하는 시점이므로
 * 여기서 VolumeSettings.ApplySaved() 로 세이브된 BGM/SFX 볼륨을 믹서에 반영한다.
 * (최초 부팅 포함 모든 씬 진입 시 저장값이 적용된다. 옵션 슬라이더 조작 중에는
 * VolumeSettings 가 BGMVolume/SFXVolume 세터를 직접 호출해 라이브 반영한다.)
 */
