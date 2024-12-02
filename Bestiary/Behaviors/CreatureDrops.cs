using HarmonyLib;
using UnityEngine;

namespace Bestiary.Behaviors;

public static class CreatureDrops
{
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.SetQuality))]
    private static class ItemDrop_SetQuality_Patch
    {
        private static void Postfix(ItemDrop __instance)
        {
            if (!__instance.m_nview || !__instance.m_nview.IsValid()) return;
            if (__instance.m_itemData.m_shared.m_canBeReparied) return;
            var num = __instance.m_nview.GetZDO().GetInt("RandomQuality".GetStableHashCode());
            int quality = num;
            if (num == 0)
            {
                quality = Random.Range(1, __instance.m_itemData.m_shared.m_maxQuality);
            }
            __instance.m_itemData.m_quality = quality;
            if (num == 0) __instance.m_itemData.m_durability = __instance.m_itemData.GetMaxDurability();
            __instance.m_nview.GetZDO().Set("RandomQuality".GetStableHashCode(), quality);
            __instance.transform.localScale = __instance.m_itemData.GetScale();
        }
    }
}