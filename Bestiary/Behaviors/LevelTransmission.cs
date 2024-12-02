using System.Collections.Generic;
using UnityEngine;

namespace Bestiary.Behaviors;

public class LevelTransmission : MonoBehaviour
{
   private Character m_character = null!;
   public List<GameObject> m_spawns = new();
   public EffectList? m_spawnEffects;
   public List<string> SpawnEffects = new();
   public void Awake()
   {
      m_character = GetComponent<Character>();
      GetSpawnEffects();
      m_character.m_onDeath += Spawn;
   }

   private void GetSpawnEffects()
   {
      m_spawnEffects = new EffectList();
      List<EffectList.EffectData> effects = new();
      foreach (var effectName in SpawnEffects)
      {
         if (ZNetScene.instance.GetPrefab(effectName) is { } prefab)
         {
            effects.Add(new EffectList.EffectData()
            {
               m_prefab = prefab
            });
         }
      }

      m_spawnEffects.m_effectPrefabs = effects.ToArray();
   }

   public void Spawn()
   {
      int level = m_character.GetLevel();
      foreach (GameObject? spawn in m_spawns)
      {
         GameObject? creature = Instantiate(spawn, transform.position, Quaternion.identity);
         if (!creature.TryGetComponent(out Character component)) continue;
         component.SetLevel(level);
         m_spawnEffects?.Create(transform.position, Quaternion.identity);
      }
   }
}