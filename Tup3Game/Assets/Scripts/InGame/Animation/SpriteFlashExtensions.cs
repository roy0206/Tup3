using System;
using DG.Tweening;
using UnityEngine;

public static class SpriteFlashExtensions
{
    public static SpriteFlash GetOrAddFlash(this SpriteRenderer renderer)
    {
        if (renderer == null) return null;
        SpriteFlash flash = renderer.GetComponent<SpriteFlash>();
        if (flash == null) flash = renderer.gameObject.AddComponent<SpriteFlash>();
        return flash;
    }

    public static Tween DOFlash(this SpriteRenderer renderer, float duration)
    {
        SpriteFlash flash = renderer.GetOrAddFlash();
        return flash == null ? null : flash.Flash(duration);
    }

    public static Tween DOFlash(this SpriteRenderer renderer, Color color, float duration)
    {
        SpriteFlash flash = renderer.GetOrAddFlash();
        return flash == null ? null : flash.Flash(color, duration);
    }

    public static Tween DOFlash(this SpriteRenderer renderer, Color color, float duration, float peak)
    {
        SpriteFlash flash = renderer.GetOrAddFlash();
        return flash == null ? null : flash.Flash(color, duration, peak);
    }

    public static Tween DOFlashTo(this SpriteRenderer renderer, float amount, float duration)
    {
        SpriteFlash flash = renderer.GetOrAddFlash();
        return flash == null ? null : flash.FlashTo(amount, duration);
    }

    public static Tween DOFlashTo(this SpriteRenderer renderer, Color color, float amount, float duration)
    {
        SpriteFlash flash = renderer.GetOrAddFlash();
        return flash == null ? null : flash.FlashTo(color, amount, duration);
    }

    public static void SetFlash(this SpriteRenderer renderer, float amount)
    {
        SpriteFlash flash = renderer.GetOrAddFlash();
        if (flash != null) flash.SetInstant(amount);
    }

    public static void SetFlash(this SpriteRenderer renderer, Color color, float amount)
    {
        SpriteFlash flash = renderer.GetOrAddFlash();
        if (flash != null) flash.SetInstant(color, amount);
    }

    public static SpriteFlashGroup GetOrAddFlashGroup(this GameObject target)
    {
        return SpriteFlashGroup.GetOrAdd(target);
    }

    public static SpriteFlashGroup GetOrAddFlashGroup(this Component target)
    {
        return target == null ? null : SpriteFlashGroup.GetOrAdd(target.gameObject);
    }

    public static Tween DOFlashAll(this GameObject target, Color color, float duration)
    {
        SpriteFlashGroup group = SpriteFlashGroup.GetOrAdd(target);
        return group == null ? null : group.Flash(color, duration);
    }

    public static Tween DOFlashAll(this GameObject target, Color color, float duration, Func<SpriteRenderer, bool> filter)
    {
        SpriteFlashGroup group = SpriteFlashGroup.GetOrAdd(target);
        if (group == null) return null;
        group.SetFilter(filter);
        return group.Flash(color, duration);
    }
}

/* [파일 노트]
 * DOTween 처럼 쓰기 위한 확장 메서드 모음이다. 컴포넌트를 미리 붙여 둘 필요 없이 SpriteRenderer 에서 바로 부른다.
 *
 *   spriteRenderer.DOFlash(Color.white, 0.12f).SetEase(Ease.OutQuad);
 *   spriteRenderer.DOFlashTo(1f, 0.2f).SetLoops(4, LoopType.Yoyo);
 *   gameObject.DOFlashAll(Color.white, 0.12f);
 *
 * GetOrAddFlash 가 SpriteFlash 컴포넌트를 없으면 즉석에서 AddComponent 한다.
 * SpriteFlash 는 [RequireComponent(typeof(SpriteRenderer))] 인데 확장 메서드의 수신자가
 * 이미 SpriteRenderer 이므로 요구 조건이 항상 만족된다.
 * 붙은 컴포넌트는 계속 남아 있어서 두 번째 호출부터는 AddComponent 비용이 없다.
 *
 * 반환값은 항상 Tween 이라 .SetEase() / .OnComplete() / .SetLoops() 를 그대로 이어붙일 수 있고,
 * PauseManager 의 DOTween.PauseAll() 에도 함께 걸린다.
 * 수신자가 null 일 때만 null 을 반환한다(파괴된 렌더러에 대고 부른 경우).
 *
 * DOFlashAll 은 자식 SpriteRenderer 전부를 한 트윈으로 묶는 SpriteFlashGroup 경로다.
 * 필터를 받는 오버로드는 최종보스처럼 "일부 자식은 빼야 하는" 경우를 위한 것이다.
 */
