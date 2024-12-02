using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bestiary.Behaviors;

public static class SkeletonHumanoid
{
    private static readonly List<string> m_skeletons = new();
    public static void Register(string prefabName) => m_skeletons.Add(prefabName);
    private static bool IsSkeleton(string prefabName) => m_skeletons.Contains(prefabName.Replace("(Clone)", string.Empty));
    private static readonly int Blocking = Animator.StringToHash("blocking");

    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    private static class Attack_Start_Patch
    {
        private static void Postfix(Attack __instance)
        {
            if (__instance.m_character == null) return;
            foreach (var character in Character.GetAllCharacters())
            {
                if (character == __instance.m_character) continue;
                if (character is not Humanoid humanoid || !IsSkeleton(character.name)) continue;
                if (humanoid.IsBlocking()) continue;
                float distance = Vector3.Distance(__instance.m_character.transform.position, humanoid.transform.position);
                var block = distance < 5f;
                humanoid.m_animator.SetBool(Blocking, block);
                humanoid.m_blocking = block;
                humanoid.m_blockTimer = 30f;
                if (!humanoid.m_nview.IsValid()) continue;
                humanoid.m_nview.GetZDO().Set(ZDOVars.s_isBlockingHash, block);
            }
        }
    }
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    private static class Character_StartAttach_Patch
    {
        private static bool Prefix(Character __instance)
        {
            if (!IsSkeleton(__instance.name) || __instance is not Humanoid humanoid) return true;
            if (humanoid.InAttack() && !humanoid.InDodge() || !humanoid.CanMove() || humanoid.IsKnockedBack() || humanoid.IsStaggering() || humanoid.InMinorAction()) return false;
                
            ItemDrop.ItemData currentWeapon = humanoid.GetCurrentWeapon();
            if (currentWeapon == null || (!currentWeapon.HaveSecondaryAttack() && !currentWeapon.HavePrimaryAttack())) return false;

            bool secondary = currentWeapon.HaveSecondaryAttack() && Random.value > 0.5f;
            if (currentWeapon.m_shared.m_skillType is Skills.SkillType.Spears) secondary = false;
            
            if (humanoid.m_currentAttack != null)
            {
                humanoid.m_currentAttack.Stop();
                humanoid.m_previousAttack = humanoid.m_currentAttack;
                humanoid.m_currentAttack = null;
            }
            
            Attack? attack = !secondary ? currentWeapon.m_shared.m_attack.Clone() : currentWeapon.m_shared.m_secondaryAttack.Clone();
            
            if (!attack.Start(humanoid, humanoid.m_body, humanoid.m_zanim, humanoid.m_animEvent, humanoid.m_visEquipment, currentWeapon, humanoid.m_previousAttack,
                    humanoid.m_timeSinceLastAttack, Random.Range(0.5f, 1f))) return false;
            
            humanoid.StartAttackGroundCheck();
            humanoid.m_currentAttack = attack;
            humanoid.m_currentAttackIsSecondary = secondary;
            humanoid.m_lastCombatTimer = 0.0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.GetMaxEitr))]
    private static class Character_GetMaxEitr_Patch
    {
        private static void Postfix(Character __instance, ref float __result)
        {
            if (!IsSkeleton(__instance.name)) return;
            __result = 9999f;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.HaveAmmo))]
    private static class Attack_HaveAmmo_Patch
    {
        private static void Postfix(Humanoid character, ref bool __result)
        {
            if (!IsSkeleton(character.name)) return;
            __result = true;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.EquipAmmoItem))]
    private static class Attack_EquipAmmoItem_Patch
    {
        private static void Prefix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
        {
            if (!IsSkeleton(character.name)) return;
            switch (weapon.m_shared.m_ammoType)
            {
                case "$ammo_arrows":
                    var arrow = ObjectDB.instance.GetItemPrefab("ArrowForgotten_RS");
                    if (arrow.TryGetComponent(out ItemDrop arrowComponent))
                    {
                        ItemDrop.ItemData? cloneArrow = arrowComponent.m_itemData.Clone();
                        character.GetInventory().AddItem(cloneArrow);
                    }
    
                    break;
                case "$ammo_bolts":
                    var bolt = ObjectDB.instance.GetItemPrefab("BoltBone");
                    if (bolt.TryGetComponent(out ItemDrop boltComponent))
                    {
                        ItemDrop.ItemData? cloneArrow = boltComponent.m_itemData.Clone();
                        character.GetInventory().AddItem(cloneArrow);
                    }
    
                    break;
            }
    
            __result = true;
        }
    }
    
}