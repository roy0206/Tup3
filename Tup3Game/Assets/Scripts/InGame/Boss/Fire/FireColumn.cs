using System;
using UnityEngine;
using DG.Tweening;

public class FireColumn : MonoBehaviour
{

    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider2D;
    void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        boxCollider2D.enabled = true;
        transform.DOMoveY(1.5f, 0.1f).OnComplete(() =>
        {
            transform.DOMoveY(-5f, 0.1f);
            boxCollider2D.enabled = false;
        });
    }

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        spriteRenderer.flipX = !spriteRenderer.flipX;
    }
}
