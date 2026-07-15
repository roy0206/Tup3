using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (T)FindAnyObjectByType(typeof(T));
                if (instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name, typeof(T));
                    instance = obj.GetComponent<T>();
                }
            }
            return instance;
        }
    }

    // Unity 가 호출하는 Awake. 파생 클래스는 이걸 오버라이드하지 말고 OnAwake() 를 사용한다.
    // (중복 인스턴스는 여기서 Destroy 되고 OnAwake() 가 호출되지 않으므로,
    //  파생 클래스가 중복 여부를 매번 검사할 필요가 없다.)
    private void Awake()
    {
        if (instance != null && instance != this as T)
        {
            Destroy(gameObject);
            return;
        }

        instance = this as T;

        if (transform.parent != null)
            DontDestroyOnLoad(transform.root.gameObject);
        else
            DontDestroyOnLoad(gameObject);

        OnAwake();
    }

    /// <summary>중복이 아닌 실제 인스턴스에서만 한 번 호출되는 초기화 훅.</summary>
    protected virtual void OnAwake() { }
}
