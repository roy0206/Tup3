using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public abstract class InteractionBase : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] protected float interactionDistance;
    public float InteractionDistance => interactionDistance;
    
    [SerializeField] private float interactionDuration;
    public float InteractionDuration => interactionDuration;

    [SerializeField] private bool interactOnce;
    
    [SerializeField] private bool hideInteractionObjects;
    
    [Header("Contents")]
    [SerializeField] private string interactionText;
    public string InteractionText => interactionText;

    [SerializeField] private string interactionSucceedSound;
    public string InteractionSucceedSound => interactionSucceedSound;
    
    [SerializeField] private string interactionFailSound;
    public string InteractionFailSound => interactionFailSound;

    private InteractionView view;
    

    private float hold = 0;

    /// <summary>false 면 InteractionManager 가 이 상호작용을 후보에서 제외해 UI(홀드 아이콘)도 뜨지 않는다.</summary>
    public virtual bool IsInteractionVisible => true;
    
    protected virtual void Start()
    {
        if (InteractionManager.Current != null) InteractionManager.Current.Register(this);
        else Debug.LogError($"[InteractionBase] 씬에 InteractionManager 가 없어 '{name}' 을(를) 등록하지 못했습니다", this);

        view = FindAnyObjectByType<InteractionView>(FindObjectsInactive.Include);
    }

    private void Update()
    {
    }

    public virtual bool OnInteract() //Call First
    {
        hold += Time.deltaTime;
        if (view != null) view.SetFill(hold/interactionDuration); // 씬 전환 중 파괴된 뷰 방어
        if(hold < interactionDuration) return false;
        hold = 0;
        if (!CanInteract())
        {
            AudioManager.Instance.PlaySound(interactionFailSound);
            return false;
        }

        AudioManager.Instance.PlaySound(interactionSucceedSound);
        if (interactOnce && InteractionManager.Current != null) InteractionManager.Current.Unregister(this);
        if(hideInteractionObjects) gameObject.GetComponent<SpriteRenderer>().enabled = false;
        return true;
    }

    public virtual void OnHoldUP()
    {
        hold = 0;
    }
    
    protected abstract bool CanInteract();
}
