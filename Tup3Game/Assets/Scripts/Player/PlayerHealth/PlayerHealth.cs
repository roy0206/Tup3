using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IHealthUIEvent
{
    [Header("체력 설정")]
    public float maxHealth = 100f;
    private float currentHealth;
    private Action<float, float> _onHealthChanged;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;
    
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (PauseManager.IsPaused) return;
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("플레이어의 현재체력" + currentHealth);
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log("플레이어의 현재체력" + currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetHealth(float amount)
    {
        currentHealth = amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }

    public event Action<float, float> OnHealthChanged;
}

/* [파일 노트]
 * TakeDamage 첫 줄의 PauseManager.IsPaused 게이트 : PlayerKnockBack 을 거치지 않고
 * PlayerHealth.TakeDamage 를 직접 부르는 경로(Fire 보스 몸통 박치기 등)까지 일시정지 중 차단한다.
 */
