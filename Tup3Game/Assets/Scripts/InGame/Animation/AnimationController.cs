using System;
using UnityEngine;
using System.Collections.Generic;


public class AnimationController : MonoBehaviour
{
    private Animation animation;
    public Animation Animation => animation;
    [SerializeField] private List<AnimationClip> animationClips;
    private void Awake()
    {
        if (!TryGetComponent<Animation>(out Animation _animation))
        {
            Debug.LogError(name + " is missing a Animation Component!");
            this.enabled = false;
        }
        animation = _animation;
    }


    public void Play(int num)
    {
        string clipName = animationClips[num].name;
        if(!animation.IsPlaying(clipName))
            animation.Play(clipName);
    }

    public void Stop(int num)
    {
        animation.Stop(animationClips[num].name);
    }

}