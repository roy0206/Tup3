using System.Collections;
using UnityEngine;

public class FreezablePlatform : MonoBehaviour
{
    [SerializeField] private float freezeDuration = 3f;
    [SerializeField] private Color frozenColor = Color.skyBlue;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private string originalTag;
    private Coroutine thawCoroutine;
    private bool initialized;

    public bool IsFrozen { get; private set; }

    private void Awake()
    {
        InitializeIfNeeded();
    }

    public void Freeze()
    {
        Freeze(freezeDuration);
    }

    public void Freeze(float duration)
    {
        InitializeIfNeeded();

        if (thawCoroutine != null)
            StopCoroutine(thawCoroutine);

        IsFrozen = true;
        gameObject.tag = "Slippery";
        SetFrozenColor();
        thawCoroutine = StartCoroutine(ThawAfter(Mathf.Max(0f, duration)));
    }

    public void ThawImmediately()
    {
        if (!initialized)
            return;

        if (thawCoroutine != null)
        {
            StopCoroutine(thawCoroutine);
            thawCoroutine = null;
        }

        IsFrozen = false;
        gameObject.tag = originalTag;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalColors[i];
        }
    }

    private IEnumerator ThawAfter(float duration)
    {
        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        thawCoroutine = null;
        ThawImmediately();
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        originalTag = gameObject.tag;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;

        initialized = true;
    }

    private void SetFrozenColor()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            Color color = frozenColor;
            color.a = originalColors[i].a;
            spriteRenderers[i].color = color;
        }
    }

    private void OnDisable()
    {
        ThawImmediately();
    }

    private void OnValidate()
    {
        freezeDuration = Mathf.Max(0f, freezeDuration);
    }
}
