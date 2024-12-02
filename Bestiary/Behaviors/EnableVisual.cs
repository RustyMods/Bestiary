using System.Collections.Generic;
using Bestiary.Managers;
using HarmonyLib;

namespace Bestiary.Behaviors;

public static class EnableVisual
{
    public static readonly Dictionary<string, FaunaManager.Critter> m_enableVisualCritters = new();
    private static readonly Dictionary<Attack, AttackVisualData> m_currentAttacks = new();

    private class AttackVisualData
    {
        public readonly string m_visualName;
        public readonly Humanoid m_character;

        public AttackVisualData(string visual, Humanoid humanoid)
        {
            m_visualName = visual;
            m_character = humanoid;
        }
    }
    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    private static class Attack_Start_Patch
    {
        private static void Postfix(Attack __instance, Humanoid character, ItemDrop.ItemData weapon, bool __result)
        {
            if (!__result) return;
            var name = character.name.Replace("(Clone)", string.Empty);

            if (!m_enableVisualCritters.TryGetValue(name, out FaunaManager.Critter data)) return;
            var sharedName = weapon.m_shared.m_name;

            if (!data.m_enableVisuals.TryGetValue(sharedName, out string visualChildName)) return;
            var visual = Utils.FindChild(character.transform, visualChildName);
            if (!visual) return;
            visual.gameObject.SetActive(true);
            
            m_currentAttacks[__instance] = new AttackVisualData(visualChildName, character);
        }
    }

    public static void UpdateAttackVisual()
    {
        List<Attack> keysToRemove = new();
        foreach (var kvp in m_currentAttacks)
        {
            if (kvp.Key.IsDone())
            {
                var visual = Utils.FindChild(kvp.Value.m_character.transform, kvp.Value.m_visualName);
                if (!visual) continue;
                visual.gameObject.SetActive(false);
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            m_currentAttacks.Remove(key);
        }
    }
}