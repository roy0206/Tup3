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
        player.OnSkillEquipChanged += HandleSkillEquipChanged;

        ApplyLockState(player.IsSkillEquiped[skillNum]);
    }

    private void OnDestroy()
    {
        if (player != null)
            player.OnSkillEquipChanged -= HandleSkillEquipChanged;
    }

    private void HandleSkillEquipChanged(int num, bool equipped)
    {
        if (num != skillNum) return;
        ApplyLockState(equipped);
    }

    private void ApplyLockState(bool equipped)
    {
        if (!equipped)
        {
            StopAllCoroutines();
            curTime = 0f;
        }

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
 *
 * ── skillNum 매핑 / 배치 순서 (2026-08-29 수정) ───────────────────────────────
 * skillNum 은 Skills 의 내부 인덱스이지 아이콘에 그려진 키 글자가 아니다. 화면 배치는 키 순서(A S D F)
 * 이고 skillNum 은 원소별 기능을 따라가므로 둘의 숫자가 어긋나 보이는 게 정상이다.
 *   화면 1번칸  A_icon (토/장벽 생성) = skillNum 1  ← skill_2_key(A)
 *   화면 2번칸  S_icon (수/회복)      = skillNum 3  ← skill_4_key(S)
 *   화면 3번칸  D_icon (화/공격속도↑) = skillNum 2  ← skill_3_key(D)
 *   화면 4번칸  F_icon (금/공격력↑)   = skillNum 0  ← skill_1_key(F)
 * 이전 프리팹은 skillNum 에 0/1/2/3 을 아이콘 순번대로 박아 넣어(=글자 순서로 채운 실수) 키를 누르면
 * 엉뚱한 아이콘의 쿨타임 게이지가 돌고 잠금 해제도 다른 칸에 표시됐다. 배치 순서도 A D F S 였다.
 * 아이콘을 추가하거나 자리를 옮길 때 skillNum 은 자리 순번이 아니라 위 표(원소↔기능)를 기준으로 정할 것.
 * 배치 순서는 SkillUI 의 HorizontalLayoutGroup 이 형제 순서대로 깔기 때문에 계층 순서로 정해진다.
 */
