using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class Dash_animation : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;


    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void DashEffect(float facingDirection)
    {
        spriteRenderer.enabled = true;
        spriteRenderer.flipX = facingDirection < 0;
        animator.SetTrigger("DashTrigger");
    }

    public void HideEffect()
    {
        spriteRenderer.enabled = false;
    }
}
