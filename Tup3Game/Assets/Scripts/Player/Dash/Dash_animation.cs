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

    public void Change_direction (float facingDirection)
    {
        Vector2 pos = this.transform.localPosition;
        pos.x = Mathf.Abs(pos.x) * -facingDirection;
        this.transform.localPosition = pos;
        spriteRenderer.flipX = facingDirection < 0;
    }

    public void HideEffect()
    {
        spriteRenderer.enabled = false;
    }
}
