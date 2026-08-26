using System;
using UnityEngine;

public class LavaPool : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (PauseManager.IsPaused) return;

        spriteRenderer.flipX = !spriteRenderer.flipX;
    }
}
