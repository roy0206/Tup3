using System;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteFlashGroup : MonoBehaviour
{
    [Header("기본값")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float flashPeak = 1f;

    [Header("대상")]
    [SerializeField] private bool includeInactive = true;

    private SpriteFlashRuntime runtime;
    private Func<SpriteRenderer, bool> filter;
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

    public int RendererCount => Runtime.RendererCount;

    public float Amount => Runtime.Amount;

    public bool IsFlashing => Runtime.IsFlashing;

    public Tween CurrentTween => Runtime.CurrentTween;

    private SpriteFlashRuntime Runtime => runtime ??= new SpriteFlashRuntime(this);

    public static SpriteFlashGroup GetOrAdd(GameObject target)
    {
        if (target == null) return null;
        SpriteFlashGroup group = target.GetComponent<SpriteFlashGroup>();
        if (group == null) group = target.AddComponent<SpriteFlashGroup>();
        return group;
    }

    public void SetFilter(Func<SpriteRenderer, bool> rendererFilter)
    {
        filter = rendererFilter;
        collected = false;
    }

    public void Collect()
    {
        collected = false;
        EnsureCollected();
    }

    public void Collect(Func<SpriteRenderer, bool> rendererFilter)
    {
        filter = rendererFilter;
        Collect();
    }

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

    private void EnsureCollected()
    {
        if (collected) return;
        collected = true;
        Runtime.SetRenderers(SpriteFlashCollector.Collect(gameObject, true, includeInactive, filter));
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
 * 자식 SpriteRenderer 여러 개를 한 덩어리로 묶어 함께 흰색으로 물들이는 컴포넌트다.
 * SpriteFlash 와 달리 루트에 SpriteRenderer 가 없어도 되고(보스 루트가 대개 그렇다),
 * "일부 렌더러만 제외"하는 필터를 코드로 넘길 수 있다.
 *
 * 필터
 *   Collect(Func<SpriteRenderer,bool>) 로 넘긴 조건이 true 인 렌더러만 대상으로 삼는다.
 *   BossBase 가 IsHitFlashRenderer 를 그대로 넘겨서 최종보스의 환영(phantom)·거합 오버레이·
 *   복제 검기 이펙트를 제외하는 데 쓴다. 필터를 바꾸면 다음 Flash 때 자동으로 다시 모은다.
 *
 * 수집 시점
 *   SpriteFlash 와 같은 이유로 Awake 가 아니라 첫 Flash(또는 명시적 Collect) 때 1회만 모은다.
 *   BossBase 는 런타임에 AddComponent 로 이 컴포넌트를 붙인 직후 필터를 지정하는데,
 *   Awake 에서 미리 모았다면 필터가 걸리기 전의 "전부 포함" 목록으로 머티리얼을 한 번 갈아끼우게 된다.
 *   지연 수집이면 그 낭비가 없다.
 *
 * 반환값은 SpriteFlash 와 동일하게 Tween 이라 .SetEase() / .OnComplete() 체이닝이 된다.
 * 여러 렌더러를 트윈 하나가 함께 구동하므로 렌더러 수만큼 트윈이 생기지 않는다.
 */
