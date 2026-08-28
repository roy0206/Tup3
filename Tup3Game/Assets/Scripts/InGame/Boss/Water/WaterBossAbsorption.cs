using UnityEngine;

public class WaterBossAbsorption : InteractionBase
{
    [SerializeField] private Water boss;
    [SerializeField] private int recoverySkillIndex = 3;

    protected override bool CanInteract()
    {
        return boss != null && boss.IsDead;
    }

    public override bool OnInteract()
    {
        if (!base.OnInteract())
            return false;

        Skills skills = FindFirstObjectByType<Skills>();
        if (skills != null)
        {
            if (recoverySkillIndex >= 0 && recoverySkillIndex < skills.IsSkillEquiped.Count)
                skills.OptainSkill(recoverySkillIndex);
            else
                Debug.LogError($"WaterBossAbsorption: 회복 스킬 인덱스 {recoverySkillIndex}가 유효하지 않습니다.", this);
        }

        if (UserDataManager.Instance != null)
        {
            UserData data = UserDataManager.Instance.Data;
            if (data != null &&
                data.Play != null &&
                recoverySkillIndex >= 0 &&
                recoverySkillIndex < data.Play.skills.Count)
            {
                data.Play.skills[recoverySkillIndex] = true;
                data.Play.clearedBosses |= BossFlag.Water;
                _ = UserDataManager.Instance.SaveAsync();
            }
        }

        return true;
    }
}
