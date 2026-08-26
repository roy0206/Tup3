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

    public virtual bool DoDamage(float damage)
    {
        if(isDead) return false;
        hp -= damage;
        OnHealthChanged?.Invoke(hp, maxHp);
        Debug.Log($"<color=green>Boss Hit! {hp} Left</color>");
        if (hp <= 0)
        {
            isDead = true;
            OnDeath?.Invoke();
        }
        return true;
    }

    public event Action<float, float> OnHealthChanged;

    public event Action OnDeath;
}

/* [파일 노트]
 * OnDeath : hp 가 0 이하가 되는 순간 1회만 발생한다. DoDamage 는 맨 위에서 isDead 를 보고 즉시 return 하므로
 * 이미 죽은 보스를 또 때려도 다시 발생하지 않는다. 보스 파생 클래스(Soil/Water/Fire/Gold)는 손대지 않았고
 * BossBase 에 이벤트만 얹은 형태라, 기존 사망 연출(각 보스의 Dead() BT 태스크)과는 독립적으로 동작한다.
 * 즉 OnDeath 는 "체력이 0 이 된 시점"이지 "사망 연출이 끝난 시점"이 아니다.
 * BossRoom 은 이 차이를 메우려고 PostCutscene 상태에서 victoryDelay 만큼 기다렸다가 승리 대사를 띄운다.
 */
