using UnityEngine;
using System.Collections;



public class PlayerKnockBack : MonoBehaviour
{

    public Playermovement movement;
    public PlayerHealth health;
    public ComboAttack combo;
    public float invincibilityDuration = 0.5f;
    private bool isInvincible = false;

    public void TakeHit(Vector2 hitOrigin, float knockbackForce, int damage)
    {
        if (isInvincible) return;
        if (movement.IsDashing()) return;

        health.TakeDamage(damage);
        if (health.IsDead) return;

        combo.CancelCombo();

        if (knockbackForce > 0f)
        {
            Vector2 direction = ((Vector2)transform.position - hitOrigin).normalized;
            float dirX = Mathf.Abs(direction.x) < 0.1f
                ? (transform.position.x >= hitOrigin.x ? 1f : -1f)
                : direction.x;

            Vector2 knockbackVelocity = new Vector2(
                dirX * knockbackForce,
                movement.GetVerticalVelocityForKnockback()
            );
            movement.ApplyKnockback(knockbackVelocity, decelSpeed: 20f);
        }

        StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        var sr = movement.spriteRenderer;
        float blinkInterval = 0.08f;
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (sr != null) sr.enabled = true;
        isInvincible = false;
    }
}
