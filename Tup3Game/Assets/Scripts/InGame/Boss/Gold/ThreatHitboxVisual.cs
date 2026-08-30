using UnityEngine;

public enum ThreatFillDirection
{
    CenterOutHorizontal,
    LeftToRight,
    BottomToTop,
}

public sealed class ThreatHitboxVisual : MonoBehaviour
{
    private GameObject visualRoot;
    private Transform fillTransform;
    private SpriteRenderer frameRenderer;
    private SpriteRenderer fillRenderer;
    private Texture2D texture;
    private Sprite frameSprite;
    private Sprite fillSprite;

    private Color threatColor = new Color(1f, 0.08f, 0.02f, 0.4f);
    private float fillAlpha = 0.7f;
    private float inset = 0.12f;
    private float pulseSpeed = 20f;
    private int sortingLayerId;
    private int sortingOrderOffset = -1;
    private Vector2 fillFullSize;
    private ThreatFillDirection fillDirection;

    public bool IsVisible => visualRoot != null && visualRoot.activeSelf;

    public void Configure(
        SpriteRenderer referenceRenderer,
        Color color,
        float targetFillAlpha,
        float borderInset,
        float targetPulseSpeed,
        int targetSortingOrderOffset)
    {
        threatColor = color;
        fillAlpha = Mathf.Clamp01(targetFillAlpha);
        inset = Mathf.Max(0f, borderInset);
        pulseSpeed = Mathf.Max(0f, targetPulseSpeed);
        sortingLayerId = referenceRenderer != null ? referenceRenderer.sortingLayerID : 0;
        sortingOrderOffset = referenceRenderer != null
            ? referenceRenderer.sortingOrder + targetSortingOrderOffset
            : targetSortingOrderOffset;

        EnsureVisual();
        ApplySorting();
    }

    public void ShowLocalBox(
        Vector2 localSize,
        Vector2 localOffset,
        ThreatFillDirection direction)
    {
        EnsureVisual();

        Vector2 fullSize = new Vector2(
            Mathf.Max(0.01f, Mathf.Abs(localSize.x)),
            Mathf.Max(0.01f, Mathf.Abs(localSize.y)));

        visualRoot.transform.localPosition = localOffset;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;
        frameRenderer.size = fullSize;
        fillFullSize = new Vector2(
            Mathf.Max(0.01f, fullSize.x - inset * 2f),
            Mathf.Max(0.01f, fullSize.y - inset * 2f));
        fillDirection = direction;

        visualRoot.SetActive(true);
        SetProgress(0f, 0f);
    }

    public void SetProgress(float progress, float elapsed)
    {
        if (!IsVisible) return;

        float ratio = Mathf.Clamp01(progress);
        float pulse = 0.9f + Mathf.Sin(elapsed * pulseSpeed) * 0.1f;

        Color frameColor = threatColor;
        frameColor.a = threatColor.a * pulse;
        frameRenderer.color = frameColor;

        fillRenderer.enabled = ratio > 0f;
        if (!fillRenderer.enabled) return;

        Vector2 currentSize = fillFullSize;
        Vector3 localPosition = Vector3.zero;

        switch (fillDirection)
        {
            case ThreatFillDirection.LeftToRight:
                currentSize.x *= ratio;
                localPosition.x = (currentSize.x - fillFullSize.x) * 0.5f;
                break;

            case ThreatFillDirection.BottomToTop:
                currentSize.y *= ratio;
                localPosition.y = (currentSize.y - fillFullSize.y) * 0.5f;
                break;

            default:
                currentSize.x *= ratio;
                break;
        }

        fillRenderer.size = new Vector2(
            Mathf.Max(0.001f, currentSize.x),
            Mathf.Max(0.001f, currentSize.y));
        fillTransform.localPosition = localPosition;

        Color currentFillColor = threatColor;
        currentFillColor.a = fillAlpha * pulse;
        fillRenderer.color = currentFillColor;
    }

    public void Hide()
    {
        if (visualRoot != null) visualRoot.SetActive(false);
    }

    private void EnsureVisual()
    {
        if (visualRoot != null) return;

        texture = new Texture2D(3, 3, TextureFormat.RGBA32, false)
        {
            name = "Threat Hitbox Visual",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave,
        };

        Color[] pixels = new Color[9];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        pixels[4] = new Color(1f, 1f, 1f, 0.35f);
        texture.SetPixels(pixels);
        texture.Apply();

        frameSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 3f, 3f),
            new Vector2(0.5f, 0.5f),
            10f,
            0,
            SpriteMeshType.FullRect,
            Vector4.one);
        frameSprite.name = "Threat Hitbox Frame";
        frameSprite.hideFlags = HideFlags.DontSave;

        fillSprite = Sprite.Create(
            texture,
            new Rect(1f, 1f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            10f);
        fillSprite.name = "Threat Hitbox Fill";
        fillSprite.hideFlags = HideFlags.DontSave;

        visualRoot = new GameObject("Threat Hitbox Visual");
        visualRoot.hideFlags = HideFlags.DontSave;
        visualRoot.transform.SetParent(transform, false);

        frameRenderer = visualRoot.AddComponent<SpriteRenderer>();
        frameRenderer.sprite = frameSprite;
        frameRenderer.drawMode = SpriteDrawMode.Sliced;

        GameObject fillObject = new GameObject("Fill");
        fillObject.hideFlags = HideFlags.DontSave;
        fillObject.transform.SetParent(visualRoot.transform, false);
        fillTransform = fillObject.transform;
        fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = fillSprite;
        fillRenderer.drawMode = SpriteDrawMode.Sliced;

        ApplySorting();
        visualRoot.SetActive(false);
    }

    private void ApplySorting()
    {
        if (frameRenderer == null || fillRenderer == null) return;

        fillRenderer.sortingLayerID = sortingLayerId;
        fillRenderer.sortingOrder = sortingOrderOffset;
        frameRenderer.sortingLayerID = sortingLayerId;
        frameRenderer.sortingOrder = sortingOrderOffset + 1;
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (fillSprite != null) Destroy(fillSprite);
        if (frameSprite != null) Destroy(frameSprite);
        if (texture != null) Destroy(texture);
    }
}

