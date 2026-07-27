using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PoolManager : Singleton<PoolManager>, ISceneEventListener
{
    [Header("Addressables")]
    [Tooltip("Preloads every prefab with this Addressables label.")]
    [SerializeField] string preloadLabel = "Pool";

    struct Entry
    {
        public string prefabName;
        public int generation;   // Get 할 때마다 증가. 지연 반납이 이미 재사용된 인스턴스를 건드리는 걸 막는다.
    }

    readonly Dictionary<string, GameObject> _prefabs = new();          // 프리팹 이름 → Addressables 로 로드한 원본
    readonly Dictionary<string, Stack<GameObject>> _pools = new();     // 프리팹 이름 → 비활성 인스턴스
    readonly Dictionary<GameObject, Entry> _owner = new();             // 인스턴스 → 소속 풀
    readonly HashSet<GameObject> _active = new();                      // 활성 인스턴스(씬 아웃 시 일괄 회수용)
    readonly List<GameObject> _toRelease = new();

    Transform _root;
    AsyncOperationHandle<IList<GameObject>> _prefabLoadHandle;
    bool _prefabLoadStarted;

    public bool PrefabsReady { get; private set; }

    protected override void OnAwake()
    {
        _root = transform;

        StartCoroutine(LoadAllPrefabs());

        SceneController.Instance.RegisterListener(this);
    }
    
    public GameObject Get(string prefabName, Vector3 position, Quaternion rotation)
    {
        if (!_prefabs.TryGetValue(prefabName, out var prefab))
        {
            WarnUnavailablePrefab(prefabName);
            return null;
        }

        var stack = GetStack(prefabName);
        
        GameObject go = null;
        while (go == null && stack.Count > 0)
            go = stack.Pop();

        if (go == null)
            go = CreateInstance(prefab, prefabName);

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        _active.Add(go);

        var entry = _owner[go];
        entry.generation++;
        _owner[go] = entry;

        return go;
    }
    
    public T Get<T>(string prefabName, Vector3 position, Quaternion rotation) where T : Component
    {
        var go = Get(prefabName, position, rotation);
        if (go == null) return null;

        var component = go.GetComponent<T>();
        if (component == null)
        {
            Debug.LogWarning($"[PoolManager] Prefab '{prefabName}' has no {typeof(T).Name} component.", go);
            Release(go);
            return null;
        }
        return component;
    }
    
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        if (!_owner.ContainsKey(instance))
        {
            Debug.LogWarning($"[PoolManager] '{instance.name}' was not spawned by the pool. Ignored.", instance);
            return;
        }
        
        if (!_active.Remove(instance)) return;

        ReturnToStack(instance);
    }

    public void Release(GameObject instance, float delay)
    {
        if (instance == null) return;

        if (delay <= 0f)
        {
            Release(instance);
            return;
        }

        if (!_owner.TryGetValue(instance, out var entry))
        {
            Debug.LogWarning($"[PoolManager] '{instance.name}' was not spawned by the pool. Ignored.", instance);
            return;
        }

        StartCoroutine(ReleaseAfter(instance, delay, entry.generation));
    }

    IEnumerator ReleaseAfter(GameObject instance, float delay, int generation)
    {
        yield return new WaitForSeconds(delay);

        if (instance == null) yield break;

        // 대기 중에 이미 반납됐다가 다시 꺼내 쓰이고 있다면 건드리지 않는다.
        if (!_owner.TryGetValue(instance, out var entry) || entry.generation != generation) yield break;

        Release(instance);
    }

    public void Prewarm(string prefabName, int count)
    {
        if (count <= 0) return;

        if (!_prefabs.TryGetValue(prefabName, out var prefab))
        {
            WarnUnavailablePrefab(prefabName);
            return;
        }

        var stack = GetStack(prefabName);
        for (int i = 0; i < count; i++)
        {
            var go = CreateInstance(prefab, prefabName);
            go.SetActive(false);
            stack.Push(go);
        }
    }

    GameObject CreateInstance(GameObject prefab, string prefabName)
    {
        var go = Instantiate(prefab, _root);
        go.name = prefabName;   // "(Clone)" 접미사 제거 — 이름을 키로 쓰므로 통일해둔다
        _owner[go] = new Entry { prefabName = prefabName };
        return go;
    }

    void ReturnToStack(GameObject instance)
    {
        if (!_owner.TryGetValue(instance, out var entry)) return;

        instance.SetActive(false);
        instance.transform.SetParent(_root, false);   // 밖에서 부모를 바꿨더라도 풀 밑으로 되돌린다
        GetStack(entry.prefabName).Push(instance);
    }

    Stack<GameObject> GetStack(string prefabName)
    {
        if (!_pools.TryGetValue(prefabName, out var stack))
        {
            stack = new Stack<GameObject>();
            _pools[prefabName] = stack;
        }
        return stack;
    }

    IEnumerator LoadAllPrefabs()
    {
        if (_prefabLoadStarted)
            yield break;

        _prefabLoadStarted = true;
        PrefabsReady = false;
        _prefabs.Clear();

        if (string.IsNullOrWhiteSpace(preloadLabel))
        {
            Debug.LogError("[PoolManager] Addressables preload label is empty.");
            yield break;
        }

        _prefabLoadHandle = Addressables.LoadAssetsAsync<GameObject>(preloadLabel, null);
        yield return _prefabLoadHandle;

        if (_prefabLoadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[PoolManager] Failed to preload prefabs with Addressables label '{preloadLabel}'. " +
                           "Make sure the prefabs are Addressable and have that label.");
            yield break;
        }

        foreach (var prefab in _prefabLoadHandle.Result)
        {
            if (prefab == null)
                continue;

            if (_prefabs.ContainsKey(prefab.name))
            {
                Debug.LogWarning($"[PoolManager] Duplicate prefab name '{prefab.name}'. " +
                                 "Prefab names must be unique; the last loaded prefab will be used.");
            }

            _prefabs[prefab.name] = prefab;
        }

        PrefabsReady = true;
        Debug.Log($"[PoolManager] Preloaded {_prefabs.Count} prefabs from Addressables label '{preloadLabel}'.");
    }

    void WarnUnavailablePrefab(string prefabName)
    {
        if (!PrefabsReady)
        {
            Debug.LogWarning($"[PoolManager] Prefab '{prefabName}' was requested before Addressables preload completed.");
            return;
        }

        Debug.LogWarning($"[PoolManager] Prefab '{prefabName}' was not found in Addressables label '{preloadLabel}'.");
    }

    void OnDestroy()
    {
        if (_prefabLoadHandle.IsValid())
            Addressables.Release(_prefabLoadHandle);

        _prefabs.Clear();
        _pools.Clear();
        _owner.Clear();
        _active.Clear();
        PrefabsReady = false;
    }

    public void OnSceneLoadComplete(string sceneName) { }
    public void OnSceneExit(string sceneName) => ReleaseAllActive();

    void ReleaseAllActive()
    {
        if (_active.Count == 0) return;

        _toRelease.Clear();
        _toRelease.AddRange(_active);
        _active.Clear();

        for (int i = 0; i < _toRelease.Count; i++)
        {
            var go = _toRelease[i];
            if (go == null) continue;   // 외부에서 파괴된 인스턴스는 버린다

            ReturnToStack(go);
        }
    }
}
