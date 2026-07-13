using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : Singleton<ScreenFader>
{
    [SerializeField] Color color = Color.black;
    [SerializeField] int sortingOrder = 5000;

    Canvas _canvas;
    CanvasGroup _group;
    Image _image;
    Tween _current;

    /// <summary>현재 페이드 알파(0=투명, 1=불투명).</summary>
    public float Alpha => _group != null ? _group.alpha : 0f;

    protected override void Awake()
    {
        base.Awake();
        if (ReferenceEquals(Instance, this)) EnsureOverlay();
    }

    void EnsureOverlay()
    {
        if (_group != null) return;

        _canvas = GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;

        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        var imgGo = new GameObject("Overlay");
        imgGo.transform.SetParent(transform, false);
        _image = imgGo.AddComponent<Image>();
        _image.raycastTarget = false;
        _image.color = new Color(color.r, color.g, color.b, 1f);

        var rt = _image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ── 페이드 API ─────────────────────────────────────────────
    /// <summary>화면을 어둡게(불투명, alpha 1).</summary>
    public Tween FadeOut(float duration, Action onComplete = null) => FadeTo(1f, duration, onComplete);

    /// <summary>화면을 밝게(투명, alpha 0).</summary>
    public Tween FadeIn(float duration, Action onComplete = null) => FadeTo(0f, duration, onComplete);

    /// <summary>임의 알파로 페이드. 진행 중 페이드는 취소된다.</summary>
    public Tween FadeTo(float targetAlpha, float duration, Action onComplete = null)
    {
        EnsureOverlay();
        _current?.Kill();

        targetAlpha = Mathf.Clamp01(targetAlpha);
        _group.blocksRaycasts = targetAlpha > 0.001f;

        _current = DOTween.To(() => _group.alpha, a => _group.alpha = a, targetAlpha, Mathf.Max(0f, duration));
        if (onComplete != null) _current.OnComplete(() => onComplete());
        return _current;
    }

    /// <summary>컷씬 Fade 스텝 호환. fadeIn=true 면 밝아짐(alpha 0), false 면 어두워짐(alpha 1).</summary>
    public Tween Fade(bool fadeIn, Color fadeColor, float duration)
    {
        SetColor(fadeColor);
        return FadeTo(fadeIn ? 0f : 1f, duration);
    }

    /// <summary>페이드 색을 변경(불투명도는 alpha 가 아니라 페이드로 제어).</summary>
    public void SetColor(Color c)
    {
        EnsureOverlay();
        color = c;
        _image.color = new Color(c.r, c.g, c.b, 1f);
    }

    /// <summary>트윈 없이 즉시 알파 설정.</summary>
    public void SetInstant(float alpha)
    {
        EnsureOverlay();
        _current?.Kill();
        _group.alpha = Mathf.Clamp01(alpha);
        _group.blocksRaycasts = _group.alpha > 0.001f;
    }
}
