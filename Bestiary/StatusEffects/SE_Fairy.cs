using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace Bestiary.StatusEffects;

public class SE_Fairy : SE_Demister
{
    public ConfigEntry<float> MaxFallSpeed = null!;
    public ConfigEntry<float> FallDamageModifier = null!;
    public ConfigEntry<float> JumpModifier = null!;
    public ConfigEntry<float> JumpStaminaUseModifier = null!;
    
    public override void ModifyWalkVelocity(ref Vector3 vel)
    {
        if (MaxFallSpeed.Value <= 0.0 ||  vel.y >= -(double) MaxFallSpeed.Value) return;
        vel.y = -MaxFallSpeed.Value;
    }
    public override void ModifyFallDamage(float baseDamage, ref float damage)
    {
        damage *= FallDamageModifier.Value;
        if (damage >= 0.0) return;
        damage = 0.0f;
    }

    public override void ModifyJump(Vector3 baseJump, ref Vector3 jump)
    {
        jump += new Vector3(baseJump.x * 0f, baseJump.y * JumpModifier.Value, baseJump.z * 0f);
    }
    
    public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse) => staminaUse += baseStaminaUse * JumpStaminaUseModifier.Value;

    public override string GetTooltipString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (m_tooltip.Length > 0) stringBuilder.AppendFormat("{0}\n", m_tooltip);
        stringBuilder.AppendFormat("$item_limitfallspeed: <color=orange>{0:0}m/s</color>\n", MaxFallSpeed.Value);
        stringBuilder.AppendFormat("$se_jumpheight: <color=orange>{0:+0;-0}%</color>\n", JumpModifier.Value * 100f);
        stringBuilder.AppendFormat("$se_jumpstamina: <color=orange>{0:+0;-0}%</color>\n", JumpStaminaUseModifier.Value * 100f);
        return Localization.instance.Localize(stringBuilder.ToString());
    }
}