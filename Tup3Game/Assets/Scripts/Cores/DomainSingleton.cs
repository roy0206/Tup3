using UnityEngine;
using UnityEngine.SceneManagement;

//특정 씬 전용 싱글톤. 씬 진입 시 생성하고, 씬 아웃 시 파괴한다.

public abstract class DomainSingleton<T> : MonoBehaviour where T : DomainSingleton<T>
{
    public static T Current { get; private set; }

    protected virtual void Awake()
    {
        if (Current != null && Current != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] 중복 인스턴스 — 새로 생성된 것을 파괴합니다.", this);
            Destroy(this);
            return;
        }
        Current = (T)this;
    }

    protected virtual void OnDestroy()
    {
        if (Current == this) Current = null;
    }
}
