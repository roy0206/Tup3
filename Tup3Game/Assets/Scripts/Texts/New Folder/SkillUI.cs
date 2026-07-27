using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml.Schema;

public class SkillUI : MonoBehaviour
{
    [Serializable]
    public class SkillSlot
    {
        public string label;
        public TextMeshProUGUI cooldownText; 

        [HideInInspector] public Func<float> getRemaining;
        [HideInInspector] public Func<float> getTotal;
    }

    [SerializeField] private Skills skills;
    public SkillSlot[] slots = new SkillSlot[4];// >>> 1234 스킬넣는곳입니당
    void Awake()
    {
        if (skills == null)
            skills = FindFirstObjectByType<Skills>();

        Bind(0, () => skills.Skill1CooldownRemaining, () => skills.Skill1CooldownTotal);
        Bind(1, () => skills.Skill2CooldownRemaining, () => skills.Skill2CooldownTotal);
        Bind(2, () => skills.Skill3CooldownRemaining, () => skills.Skill3CooldownTotal);
        Bind(3, () => skills.Skill4CooldownRemaining, () => skills.Skill4CooldownTotal);
    }

    void Bind(int i, Func<float> remaining, Func<float> total)
    {
        if (i < slots.Length && slots[i] != null)
        {
            slots[i].getRemaining = remaining;
            slots[i].getTotal = total;
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var slot in slots)
        {
            if (slot == null || slot.getRemaining == null) continue;

            float remaining = slot.getRemaining();
            Debug.Log($"{slot.label} remaining={remaining}");
            float total = slot.getTotal();
            
            bool onCooldown = remaining > 0f && total > 0f;
            if (slot.cooldownText != null)
            {
                slot.cooldownText.text = $"[skill!] {Format(remaining,total)}\n";
            }
        }
    }
    string Format(float remaining, float total)
    {
        if (remaining > 0f)
            return Mathf.Ceil(remaining) + "s/" + Mathf.Ceil(total) + "s";   // 쿨다운 중: 남은 초
        return "Ready";                            // 사용 가능
    }
}
