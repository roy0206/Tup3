using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlash : MonoBehaviour
{
    [Header("기본값")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float flashPeak = 1f;

    [Header("대상")]
    [SerializeField] private bool includeChildren = false;
    [SerializeField] private bool includeInactive = true;

    private SpriteFlashRuntime runtime;
    private bool collected;

    public Color FlashColor
    {
        get => flashColor;
        set => flashColor = value;
    }

    public float FlashDuration
    {
        get => flashDuration;
        set => flashDuration = value;
    }

    public float FlashPeak
    {
        get => flashPeak;
        set => flashPeak = Mathf.Clamp01(value);
    }

    public bool IncludeChildren
    {
        get => includeChildren;
        set
        {
            if (includeChildren == value) return;
            includeChildren = value;
            Refresh();
        }
    }

    public float Amount => Runtime.Amount;

    public bool IsFlashing => Runtime.IsFlashing;

    public Tween CurrentTween => Runtime.CurrentTween;

    private SpriteFlashRuntime Runtime => runtime ??= new SpriteFlashRuntime(this);

    public Tween Flash()
    {
        return Flash(flashColor, flashDuration, flashPeak);
    }

    public Tween Flash(float duration)
    {
        return Flash(flashColor, duration, flashPeak);
    }

    public Tween Flash(Color color, float duration)
    {
        return Flash(color, duration, flashPeak);
    }

    public Tween Flash(Color color, float duration, float peak)
    {
        EnsureCollected();
        return Runtime.Flash(color, duration, peak);
    }

    public Tween FlashTo(float amount, float duration)
    {
        return FlashTo(flashColor, amount, duration);
    }

    public Tween FlashTo(Color color, float amount, float duration)
    {
        EnsureCollected();
        return Runtime.FlashTo(color, amount, duration);
    }

    public void SetInstant(float amount)
    {
        EnsureCollected();
        Runtime.SetInstant(flashColor, amount);
    }

    public void SetInstant(Color color, float amount)
    {
        EnsureCollected();
        Runtime.SetInstant(color, amount);
    }

    public void Stop()
    {
        Runtime.Stop();
    }

    public void Clear()
    {
        Runtime.Clear();
    }

    public void Refresh()
    {
        collected = false;
        EnsureCollected();
    }

    private void EnsureCollected()
    {
        if (collected) return;
        collected = true;
        Runtime.SetRenderers(SpriteFlashCollector.Collect(gameObject, includeChildren, includeInactive, null));
    }

    private void OnDisable()
    {
        if (runtime == null) return;
        runtime.Clear();
    }

    private void OnDestroy()
    {
        if (runtime == null) return;
        runtime.KillTween();
    }
}

/* [파일 노트]
 * 스프라이트 하나(또는 자기 자신 + 자식들)를 흰색으로 물들이는 컴포넌트다.
 * SpriteRenderer 에 의존하므로 RequireComponent 로 강제하고, 한 오브젝트에 둘 이상 붙지 않게 DisallowMultipleComponent 를 건다.
 * 실제 동작은 SpriteFlashCore.cs 의 SpriteFlashRuntime 이 담당한다.
 *
 * 반환값이 Tween 인 이유
 *   프로젝트가 DOTween 을 전역으로 쓰므로 호출부에서 DOTween 문법을 그대로 이어붙일 수 있어야 한다.
 *     sr.GetComponent<SpriteFlash>().Flash(0.15f).SetEase(Ease.OutQuad).OnComplete(DoSomething);
 *     flash.FlashTo(1f, 0.2f).SetLoops(4, LoopType.Yoyo);
 *   일시정지도 DOTween 기반이라 PauseManager 의 DOTween.PauseAll() 에 자동으로 함께 멈춘다.
 *
 * 지연 수집
 *   Awake 에서 렌더러를 모으지 않고 첫 Flash 때 1회만 모은다.
 *   런타임에 AddComponent 로 붙였을 때(확장 메서드 경로) Awake 가 즉시 돌면서 불필요한 머티리얼 교체를
 *   하는 것을 피하기 위해서다. 자식 구성이 런타임에 바뀌었다면 Refresh() 로 다시 모은다.
 *
 * includeChildren
 *   본 리깅 스프라이트(토보스 등)처럼 SpriteRenderer 가 여러 개인 경우를 위한 옵션이다.
 *   루트에 SpriteRenderer 가 아예 없거나 "일부 자식만 제외"가 필요하면 SpriteFlashGroup 을 쓴다.
 *
 * OnDisable 에서 Clear
 *   흰색으로 물든 채 비활성화되면 다시 켰을 때 그대로 하얗게 남는다. 꺼질 때 트윈을 죽이고 강도를 0 으로 되돌린다.
 */
