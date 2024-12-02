using Bestiary.Managers;

namespace Bestiary.StatusEffects;

public class Critter : StatusEffect
{
    public FaunaManager.Critter m_data = null!;

    public override void OnDamaged(HitData hit, Character attacker)
    {
        hit.ApplyModifier(m_data.configs.ArmorConfig.Value);
    }

    public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
    {
        hitData.ApplyModifier(m_data.configs.DamageConfig.Value);
    }
}