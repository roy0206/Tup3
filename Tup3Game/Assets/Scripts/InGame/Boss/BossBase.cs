using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections.Generic;

public abstract class BossBase : MonoBehaviour
{
    [SerializeField] protected BehaviorTree behaviorTree;
    [SerializeField] protected List<BoxCollider2D> boxColliders = new();
    [SerializeField] private float maxHp;

    protected AnimationController animationController;
    
    private float hp;
    public float Hp => hp;

    private bool isDead = false;
    public bool IsDead => isDead;
        
    
    public void DoDamage(float damage)
    {
        if(isDead) return;
        hp -= damage;
        if (hp <= 0)
        {
            OnDead();
            isDead = true;
        }
    }

    protected abstract void OnDead();

}
