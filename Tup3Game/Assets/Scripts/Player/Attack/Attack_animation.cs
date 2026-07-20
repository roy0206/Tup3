using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]

public class Attack_animation : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;


    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void PlayEffect(int comboStep, float facingDirection)
    {
        spriteRenderer.enabled = true;
        spriteRenderer.flipX = facingDirection < 0;
        switch (comboStep)
        {
            case 1:
                animator.SetTrigger("Combo1");
                break;
            case 2:
                animator.SetTrigger("Combo2");
                break;
            case 3:
                animator.SetTrigger("Combo3");
                break;
        }
    }

    public void HideEffect()
    {
        spriteRenderer.enabled = false;
    }
}
