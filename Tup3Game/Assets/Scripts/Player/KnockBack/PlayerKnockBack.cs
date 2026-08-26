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
        if (PauseManager.IsPaused) return;
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

/* [파일 노트]
 * TakeHit 첫 줄의 PauseManager.IsPaused 게이트 : 보스 패턴/투사체/함정의 피해가 전부 이 메서드를
 * 경유하므로, 일시정지 중에는 어떤 경로로도 플레이어가 넉백/피해를 받지 않는다(전역 피해 차단 지점).
 */
