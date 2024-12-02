using System.Collections.Generic;
using Bestiary.Managers;
using HarmonyLib;
using UnityEngine;

namespace Bestiary.Behaviors;

public static class OnDeath
{
    public static readonly Dictionary<string, FaunaManager.Critter> m_delayedDeathCritters = new();
    private static readonly List<Character> m_dyingCharacters = new();

    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    private static class Character_OnDeath_Patch
    {
        private static bool Prefix(Character __instance)
        {
            var name = __instance.name.Replace("(Clone)", string.Empty);
            if (!m_delayedDeathCritters.TryGetValue(name, out FaunaManager.Critter critter)) return true;
            if (m_dyingCharacters.Contains(__instance))
            {
                m_dyingCharacters.Remove(__instance);
                return true;
            }
            m_dyingCharacters.Add(__instance);
            __instance.m_baseAI.StopMoving();
            Transform transform = __instance.transform;
            critter.OnDeathEffects?.Create(transform.position, transform.rotation, transform);
            __instance.m_zanim.SetTrigger(critter.DyingTrigger);
            __instance.Invoke(nameof(Character.OnDeath), critter.OnDeathDelay);
            
            return false;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.CheckDeath))]
    private static class Character_CheckDeath_Patch
    {
        private static bool Prefix(Character __instance)
        {
            return !m_dyingCharacters.Contains(__instance);
        }
    }

    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.UpdateAI))]
    private static class BaseAI_UpdateAI_Patch
    {
        private static bool Prefix(BaseAI __instance)
        {
            return !m_dyingCharacters.Contains(__instance.m_character);
        }
    }
    
}