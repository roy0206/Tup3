using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkillView : MonoUI
{
    [SerializeField] private int skillNum;

    private Skills player;
    private float curTime = 0;
    
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Skills>();
        player.OnSkillsActive[skillNum] += (duration, cooltime)=> StartCoroutine(OnSkillsActive(duration, cooltime));
    }

    IEnumerator OnSkillsActive(float duration, float cooltime)
    {
        curTime = duration;
        image.color = new Color(0f, 01f, 0f, 1);
        while (curTime > 0)
        {
            curTime -= Time.deltaTime;
            image.fillAmount = curTime / duration;
            yield return null;
        }
        curTime = cooltime;
        image.color = new Color(0.5f, 0.5f, 0.5f, 1);
        while (curTime > 0)
        {
            curTime -= Time.deltaTime;
            image.fillAmount = 1 - curTime / cooltime;
            yield return null;
        }
        image.color = Color.white;
    }
}
