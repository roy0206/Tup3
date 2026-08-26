using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkillView : MonoUI
{
    [SerializeField] private int skillNum;
    [SerializeField] private Color lockedColor = new(0.2f, 0.2f, 0.2f, 0.9f);

    private Skills player;
    private float curTime = 0;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Skills>();
        player.OnSkillsActive[skillNum] += (duration, cooltime)=> StartCoroutine(OnSkillsActive(duration, cooltime));
        player.OnSkillEquipped += HandleSkillEquipped;

        ApplyLockState(player.IsSkillEquiped[skillNum]);
    }

    private void OnDestroy()
    {
        if (player != null)
            player.OnSkillEquipped -= HandleSkillEquipped;
    }

    private void HandleSkillEquipped(int num)
    {
        if (num != skillNum) return;
        ApplyLockState(true);
    }

    private void ApplyLockState(bool equipped)
    {
        image.color = equipped ? Color.white : lockedColor;
        if (!equipped)
            image.fillAmount = 1f;
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

/* [파일 노트]
 * 보스 씬의 스킬 슬롯 아이콘. 발동/쿨타임 게이지(OnSkillsActive)에 더해
 * 보유 여부 표시를 담당한다 — 미획득 스킬은 lockedColor 로 어둡게 잠금 표시되고,
 * Skills.OnSkillEquipped(획득 또는 세이브 동기화) 를 받으면 원색으로 활성화된다.
 * 미획득 상태에서는 Skills 쪽 입력 게이트 때문에 발동 자체가 불가능하므로
 * 잠금 표시와 쿨타임 게이지가 겹칠 일은 없다.
 */
