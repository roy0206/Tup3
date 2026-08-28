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
    
    protected virtual void Start()
    {
        InteractionManager.Current.Register(this);
        view = FindAnyObjectByType<InteractionView>();
    }

    private void Update()
    {
    }

    public virtual bool OnInteract() //Call First
    {
        hold += Time.deltaTime;
        view.SetFill(hold/interactionDuration);
        if(hold < interactionDuration) return false;
        hold = 0;
        if (!CanInteract())
        {
            AudioManager.Instance.PlaySound(interactionFailSound);
            return false;
        }

        AudioManager.Instance.PlaySound(interactionSucceedSound);
        if(interactOnce) InteractionManager.Current.Unregister(this);
        if(hideInteractionObjects) gameObject.GetComponent<SpriteRenderer>().enabled = false;
        return true;
    }

    public virtual void OnHoldUP()
    {
        hold = 0;
    }
    
    protected abstract bool CanInteract();
}
