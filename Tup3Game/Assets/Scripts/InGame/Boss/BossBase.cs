using System;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections.Generic;

public abstract class BossBase : MonoBehaviour, IHealthUIEvent
{
    [SerializeField] protected BehaviorTree behaviorTree;
    [SerializeField] protected List<BoxCollider2D> boxColliders = new();
    [SerializeField] private float maxHp;

    protected AnimationController animationController;
    
    private float hp;
    public float Hp => hp;

    private bool isDead = false;
    public bool IsDead => isDead;

    protected void Awake()
    {
        hp = maxHp;
    }

    public void DoDamage(float damage)
    {
        if(isDead) return;
        hp -= damage;
        OnHealthChanged?.Invoke(hp, maxHp);
        Debug.Log($"<color=green>Boss Hit! {hp} Left</color>");
        if (hp <= 0)
        {
            isDead = true;
        }
    }

    public event Action<float, float> OnHealthChanged;
}
