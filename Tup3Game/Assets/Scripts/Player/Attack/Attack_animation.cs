using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]

public class Attack_animation : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public float combo1ClipLength = 0.25f;
    public float combo2ClipLength = 0.25f;
    public float combo3ClipLength = 0.25f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void PlayEffect(int comboStep, float facingDirection, float targetDuration)
    {
        spriteRenderer.enabled = true;
        spriteRenderer.flipX = facingDirection < 0;
        float clipLength = comboStep switch
        {
            1 => combo1ClipLength,
            2 => combo2ClipLength,
            3 => combo3ClipLength,
            _ => 1f
        };
        animator.speed = targetDuration > 0f ? clipLength / targetDuration : 1f;

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
        animator.speed = 1f;
    }
}
