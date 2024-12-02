using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Bestiary.Behaviors;
using Bestiary.StatusEffects;
using HarmonyLib;
using ItemManager;
using UnityEngine;

namespace Bestiary.Managers;

public class FaunaManager
{
    private static readonly Dictionary<string, Critter> m_critters = new();
    private static readonly List<Bird> m_birds = new();
    private static readonly List<GameObject> m_extras = new();
    private static readonly List<ProjectileData> m_projectiles = new();

    private static readonly Dictionary<string, GameObject> m_specialEffects = new();

    public enum TimeOfDay
    {
        None = 0, 
        Day = 1, 
        Night = 2, 
        Both = 3
    }

    private static SpawnSystemList m_spawnList = null!;

    static FaunaManager()
    {
        Harmony harmony = new("org.bepinex.helpers.FaunaManager");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(FaunaManager), nameof(Patch_FejdStartup))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(FaunaManager), nameof(Patch_ZNetScene_Awake))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(SpawnSystem), nameof(SpawnSystem.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(FaunaManager), nameof(Patch_SpawnSystem_Awake))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(FaunaManager), nameof(Patch_ObjectDB_Awake))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(FaunaManager), nameof(Patch_CharacterAwake))));
    }

    internal static void Patch_FejdStartup()
    {
        foreach(Critter? critter in m_critters.Values) critter.SetupConfigs();
        foreach(Bird bird in m_birds) bird.SetupConfigs();
    }
    
    internal static void Patch_ZNetScene_Awake()
    {
        CacheSpecialEffects();
        RegisterExtraAssets();
        RegisterProjectiles();
        m_spawnList = BestiaryPlugin._Root.AddComponent<SpawnSystemList>();
        LoadCreatures();
        LoadBirds();
    }

    private static void CacheSpecialEffects()
    {
        if (ZNetScene.instance.GetPrefab("Fenring") is { } fenring && fenring.TryGetComponent(out Humanoid fenringHumanoid))
        {
            foreach (var effect in fenringHumanoid.m_waterEffects.m_effectPrefabs)
            {
                if (effect.m_prefab.name is "vfx_water_surface")
                    m_specialEffects[effect.m_prefab.name] = effect.m_prefab;
            }
        }

        if (ZNetScene.instance.GetPrefab("Serpent") is { } serpent && serpent.TryGetComponent(out Humanoid serpentHumanoid))
        {
            foreach (var effect in serpentHumanoid.m_waterEffects.m_effectPrefabs)
            {
                if (effect.m_prefab.name is "vfx_serpent_watersurface")
                    m_specialEffects[effect.m_prefab.name] = effect.m_prefab;
            }
        }
    }
    
    [HarmonyPriority(Priority.Last)]
    internal static void Patch_ObjectDB_Awake(ObjectDB __instance)
    {
        if (!ZNetScene.instance) return;
        foreach (Critter? critter in m_critters.Values)
        {
            critter.RegisterStatusEffect();
            critter.AddCharacterDrops();
            critter.AddConsumeItems();
            critter.RegisterItems();
        }
        foreach(Bird bird in m_birds) bird.SetupDrops();
        BestiaryPlugin._Plugin.SetupWatcher();
    }
    
    internal static void Patch_SpawnSystem_Awake(SpawnSystem __instance)
    {
        if (!BestiaryPlugin._Root.TryGetComponent(out SpawnSystemList spawnSystemList)) return;
        __instance.m_spawnLists.Add(spawnSystemList);
    }

    internal static void Patch_CharacterAwake(Character __instance)
    {
        if (!__instance || !m_critters.TryGetValue(__instance.name.Replace("(Clone)", string.Empty), out Critter data)) return;
        if (data.CreatureStatusEffect != null) __instance.GetSEMan().AddStatusEffect(data.CreatureStatusEffect);
    }

    private static void RegisterItemsToDB(GameObject[] list)
    {
        foreach (GameObject item in list)
        {
            if (!item.GetComponent<ZNetView>()) continue;
            RegisterToDB(item);
            Register(item);
        }
    }

    private static void RegisterToDB(GameObject prefab)
    {
        if (!ObjectDB.instance.m_items.Contains(prefab)) ObjectDB.instance.m_items.Add(prefab);
        if (!ObjectDB.instance.m_itemByHash.ContainsKey(prefab.name.GetStableHashCode())) 
            ObjectDB.instance.m_itemByHash[prefab.name.GetStableHashCode()] = prefab;
    }

    public static GameObject? RegisterAssetToZNetScene(string assetName, AssetBundle bundle)
    {
        if (bundle.LoadAsset<GameObject>(assetName) is { } prefab)
        {
            m_extras.Add(prefab);
            return prefab;
        }
        Debug.LogWarning(assetName + " is null");
        return null;
    }

    private static void LoadCreatures()
    {
        foreach (Critter critter in m_critters.Values)
        {
            critter.Setup();
            Register(critter.Prefab);
            void SettingChanged()
            {
                if (m_spawnList.m_spawners.Find(x => x.m_name == critter.Prefab.name) is { } spawnData)
                {
                    spawnData.m_biome =  critter.configs.BiomeConfig.Value;
                    spawnData.m_spawnInterval = critter.configs.IntervalConfig.Value;
                    spawnData.m_maxSpawned = critter.configs.MaxSpawnConfig.Value;
                    spawnData.m_spawnAtNight = critter.configs.SpawnTimeConfig.Value is TimeOfDay.Both or TimeOfDay.Night;
                    spawnData.m_spawnAtDay = critter.configs.SpawnTimeConfig.Value is TimeOfDay.Both or TimeOfDay.Day;
                }
            }
            critter.configs.BiomeConfig.SettingChanged += (_, _) => SettingChanged();
            critter.configs.IntervalConfig.SettingChanged += (_, _) => SettingChanged();
            critter.configs.MaxSpawnConfig.SettingChanged += (_, _) => SettingChanged();
            critter.configs.SpawnTimeConfig.SettingChanged += (_, _) => SettingChanged();
            critter.configs.SpawnTimeConfig.SettingChanged += (_, _) => SettingChanged();
            m_spawnList.m_spawners.Add(critter.GetSpawnData());
        }
    }
    
    private static void LoadBirds()
    {
        foreach (Bird? bird in m_birds)
        {
            bird.Setup();
            Register(bird.Prefab);

            void SettingChanged()
            {
                if (m_spawnList.m_spawners.Find(x => x.m_name == bird.Prefab.name) is not { } spawnData) return;
                spawnData.m_biome = bird.configs.BiomeConfig.Value;
                spawnData.m_maxSpawned = bird.configs.MaxSpawnedConfig.Value;
                spawnData.m_spawnInterval = bird.configs.IntervalConfig.Value;
            }

            bird.configs.BiomeConfig.SettingChanged += (_, _) => SettingChanged();
            bird.configs.MaxSpawnedConfig.SettingChanged += (_, _) => SettingChanged();
            bird.configs.IntervalConfig.SettingChanged += (_, _) => SettingChanged();
            m_spawnList.m_spawners.Add(bird.GetSpawnData());
        }
    }

    private static void UpdateEffectList(List<string> effects, ref EffectList effectList)
    {
        if (effects.Count <= 0) return;
        List<EffectList.EffectData> originals = effectList.m_effectPrefabs.ToList();
        foreach (var effect in effects)
        {
            if (m_specialEffects.TryGetValue(effect, out GameObject specialEffect))
            {
                originals.Add(new EffectList.EffectData()
                {
                    m_prefab = specialEffect,
                    m_attach = true
                });
            }
            else if (ZNetScene.instance.GetPrefab(effect) is { } prefab)
            {
                originals.Add(new EffectList.EffectData()
                {
                    m_prefab = prefab,
                    m_enabled = true
                });
            }
        }

        effectList.m_effectPrefabs = originals.ToArray();
    }
    
    private static void RegisterItems(GameObject[] list)
    {
        foreach (var item in list)
        {
            if (item.GetComponent<ZNetView>()) Register(item);
            if (item.TryGetComponent(out ItemDrop component))
            {
                RegisterEffectList(component.m_itemData.m_shared.m_hitEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_hitTerrainEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_blockEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_startEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_holdStartEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_equipEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_unequipEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_triggerEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_trailStartEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_buildEffect);
                RegisterEffectList(component.m_itemData.m_shared.m_destroyEffect);
                if (component.m_itemData.m_shared.m_attack.m_attackProjectile is {} attackProjectile) Register(attackProjectile);
            }
        }
    }

    private static void RegisterEffectList(EffectList effectList)
    {
        foreach (var effect in effectList.m_effectPrefabs)
        {
            if (effect.m_prefab != null && effect.m_prefab.GetComponent<ZNetView>())
            {
                Register(effect.m_prefab);
            }
        }
    }

    private static void UpdateItems(List<string> itemList, ref GameObject[] originals)
    {
        var list = originals.ToList();
        foreach (var itemName in itemList)
        {
            if (ObjectDB.instance.GetItemPrefab(itemName) is { } itemPrefab)
            {
                list.Add(itemPrefab);
            }
        }

        originals = list.ToArray();
    }
    
    private static void RegisterExtraAssets()
    {
        foreach (GameObject asset in m_extras) Register(asset);
    }

    private static void RegisterProjectiles()
    {
        foreach (ProjectileData? projectile in m_projectiles)
        {
            if (projectile.Prefab.TryGetComponent(out Projectile component))
            {
                RegisterEffectList(component.m_hitEffects);
                RegisterEffectList(component.m_hitWaterEffects);
                RegisterEffectList(component.m_spawnOnHitEffects);
                if (!projectile.SpawnOnHit.IsNullOrWhiteSpace() && ZNetScene.instance.GetPrefab(projectile.SpawnOnHit) is { } spawnOnHit)
                {
                    component.m_spawnOnHit = spawnOnHit;
                    component.m_spawnOnHitChance = projectile.SpawnOnHitChance;
                }
                
                UpdateEffectList(projectile.HitEffects, ref component.m_hitEffects);
                UpdateEffectList(projectile.HitWaterEffects, ref component.m_hitWaterEffects);
                UpdateRandomSpawnOnHit(projectile.RandomSpawnOnHit, ref component.m_randomSpawnOnHit);
            }

            Register(projectile.Prefab);
        }
    }

    private static void UpdateRandomSpawnOnHit(List<string> prefabNames, ref List<GameObject> data)
    {
        if (prefabNames.Count <= 0) return;
        foreach (var prefabName in prefabNames)
        {
            if (ZNetScene.instance.GetPrefab(prefabName) is { } prefab)
            {
                data.Add(prefab);
            }
        }
    }

    private static void Register(GameObject prefab)
    {
        if (!ZNetScene.instance.m_prefabs.Contains(prefab)) ZNetScene.instance.m_prefabs.Add(prefab);
        if (!ZNetScene.instance.m_namedPrefabs.ContainsKey(prefab.name.GetStableHashCode()))
        {
            ZNetScene.instance.m_namedPrefabs[prefab.name.GetStableHashCode()] = prefab;
        }
    }
    
    public class ProjectileData
    {
        public readonly GameObject Prefab = null!;
        public readonly List<string> HitEffects = new();
        public readonly List<string> HitWaterEffects = new();
        public string SpawnOnHit = "";
        public float SpawnOnHitChance = 1f;
        public readonly List<string> RandomSpawnOnHit = new();

        public ProjectileData(string name, AssetBundle bundle)
        {
            if (bundle.LoadAsset<GameObject>(name) is { } prefab)
            {
                Prefab = prefab;
                m_projectiles.Add(this);
            }
            else
            {
                Debug.LogWarning(name + " is null");
            }
        }
    }
    public class Bird
    {
        public readonly GameObject Prefab = null!;
        public float Health = 1;
        public float Range = 10f;
        public float Speed = 4;
        public Heightmap.Biome Biome = Heightmap.Biome.None;
        public readonly Heightmap.BiomeArea BiomeArea = Heightmap.BiomeArea.Everything;
        public int MaxSpawned = 1;
        public float SpawnInterval = 100f;
        public float SpawnDistance = 50f;
        public float SpawnRadiusMin = 10f;
        public float SpawnRadiusMax = 100f;
        public string RequiredGlobalKey = "";
        public List<string> RequiredEnvironments = new();
        public int GroupSizeMin = 0;
        public int GroupSizeMax = 1;
        public float GroupRadius = 50f;
        public TimeOfDay SpawnTimeOfDay = TimeOfDay.Both;
        public float MinAltitude = -1000f;
        public float MaxAltitude = 1000f;
        public float MinTilt = 0f;
        public float MaxTilt = 50f;
        public float GroundOffset = 0.5f;
        public readonly List<string> DestroyedEffects = new();
        public readonly CritterDrops Drops = new();
        public readonly BirdConfigs configs = new();
        public Bird(string name, AssetBundle assetBundle)
        {
            if (assetBundle.LoadAsset<GameObject>(name) is { } prefab)
            {
                Prefab = prefab;
                GetData();
                m_birds.Add(this);
            }
            else
            {
                Debug.LogWarning(name + " is null");
            }
        }

        private void GetData()
        {
            if (Prefab.TryGetComponent(out Destructible destructible))
            {
                Health = destructible.m_health;
            }
        }

        public void SetupConfigs()
        {
            configs.BiomeConfig = BestiaryPlugin._Plugin.config(Prefab.name, "1. Biome", Biome, "Set available biomes for creature to spawn in");
            configs.MaxSpawnedConfig = BestiaryPlugin._Plugin.config(Prefab.name, "2. Max Spawn", MaxSpawned, "Set maximum amount per zone");
            configs.IntervalConfig = BestiaryPlugin._Plugin.config(Prefab.name, "3. Spawn Interval", SpawnInterval, "Set interval between check to spawn");
            configs.TimeOfDayConfig = BestiaryPlugin._Plugin.config(Prefab.name, "4. Time Of Day", SpawnTimeOfDay, "Set time of day creature should spawn");
            configs.HealthConfig = BestiaryPlugin._Plugin.config(Prefab.name, "5. Health", Health, "Set health of bird");
            string FormatDropData()
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (var index = 0; index < Drops.m_dropData.Count; index++)
                {
                    CritterDrops.DropData? drop = Drops.m_dropData[index];
                    stringBuilder.AppendFormat("{0}:{1}:{2}:{3}", drop.m_prefabName, drop.m_min, drop.m_max, drop.m_chance);
                    if (index >= Drops.m_dropData.Count - 1) continue;
                    stringBuilder.Append(",");
                }

                return stringBuilder.ToString();
            }
            configs.DropConfig = BestiaryPlugin._Plugin.config(Prefab.name, "6. Drops", FormatDropData(), "Define drop list, [prefabName]:[min]:[max]:[weight], seperated by commas");
        }

        public void Setup()
        {
            if (!ZNetScene.instance) return;
            if (Prefab.TryGetComponent(out RandomFlyingBird randomFlyingBird))
            {
                randomFlyingBird.m_flyRange = Range;
                randomFlyingBird.m_minAlt = 5f;
                randomFlyingBird.m_maxAlt = 20f;
                randomFlyingBird.m_speed = Speed;
                randomFlyingBird.m_turnRate = 10f;
                randomFlyingBird.m_wpDuration = 1f;
                randomFlyingBird.m_flapDuration = 2f;
                randomFlyingBird.m_sailDuration = 0.2f;
                randomFlyingBird.m_landChance = 0.2f;
                randomFlyingBird.m_landDuration = 10f;
                randomFlyingBird.m_avoidDangerDistance = 10f;
                randomFlyingBird.m_noRandomFlightAtNight = false;
                randomFlyingBird.m_randomNoiseIntervalMin = 5f;
                randomFlyingBird.m_randomNoiseIntervalMax = 10f;
                randomFlyingBird.m_noNoiseAtNight = true;
                randomFlyingBird.m_singleModel = true;
            }

            if (Prefab.TryGetComponent(out Destructible destructible))
            {
                destructible.m_health = configs.HealthConfig.Value;
                configs.HealthConfig.SettingChanged += (_, _) => destructible.m_health = configs.HealthConfig.Value;
                RegisterEffectList(destructible.m_destroyedEffect);
                RegisterEffectList(destructible.m_hitEffect);
                UpdateEffectList(DestroyedEffects, ref destructible.m_destroyedEffect);
            }
        }

        public void SetupDrops()
        {
            if (!ObjectDB.instance) return;
            if (Prefab.TryGetComponent(out DropOnDestroyed dropOnDestroyed))
            {
                dropOnDestroyed.m_dropWhenDestroyed.m_drops = configs.GetDrops();
                configs.DropConfig.SettingChanged += (_,_) => dropOnDestroyed.m_dropWhenDestroyed.m_drops = configs.GetDrops();
            }
        }

        public class BirdConfigs
        {
            public ConfigEntry<float> HealthConfig = null!;
            public ConfigEntry<Heightmap.Biome> BiomeConfig = null!;
            public ConfigEntry<int> MaxSpawnedConfig = null!;
            public ConfigEntry<float> IntervalConfig = null!;
            public ConfigEntry<string> DropConfig = null!;
            public ConfigEntry<TimeOfDay> TimeOfDayConfig = null!;
            public List<DropTable.DropData> GetDrops()
            {
                List<DropTable.DropData> data = new();
                if (!ObjectDB.instance) return data;
                foreach (var dropInfo in DropConfig.Value.Split(':'))
                {
                    var info = dropInfo.Split(':');
                    if (info.Length < 4) continue;
                    if (ObjectDB.instance.GetItemPrefab(info[0]) is { } prefab)
                    {
                        data.Add(new DropTable.DropData()
                        {
                            m_item = prefab,
                            m_stackMin = int.TryParse(info[1], out int min) ? min : 1,
                            m_stackMax = int.TryParse(info[2], out int max) ? max : 1,
                            m_weight = float.TryParse(info[3], out float chance) ? chance : 1f,
                        });
                    }
                }
                return data;
            }
        }

        public SpawnSystem.SpawnData GetSpawnData()
        {
            return new SpawnSystem.SpawnData
            {
                m_name = Prefab.name,
                m_enabled = true,
                m_prefab = Prefab,
                m_biome = configs.BiomeConfig.Value,
                m_biomeArea = BiomeArea,
                m_maxSpawned = configs.MaxSpawnedConfig.Value,
                m_spawnInterval = configs.IntervalConfig.Value,
                m_spawnDistance = SpawnDistance,
                m_spawnRadiusMin = SpawnRadiusMin,
                m_spawnRadiusMax = SpawnRadiusMax,
                m_requiredGlobalKey = RequiredGlobalKey,
                m_requiredEnvironments = RequiredEnvironments,
                m_groupSizeMin = GroupSizeMin,
                m_groupSizeMax = GroupSizeMax,
                m_groupRadius = GroupRadius,
                m_spawnAtNight = configs.TimeOfDayConfig.Value is TimeOfDay.Both or TimeOfDay.Night,
                m_spawnAtDay = configs.TimeOfDayConfig.Value is TimeOfDay.Both or TimeOfDay.Day,
                m_minAltitude = MinAltitude,
                m_maxAltitude = MaxAltitude,
                m_minTilt = MinTilt,
                m_maxTilt = MaxTilt,
                m_huntPlayer = false,
                m_groundOffset = GroundOffset,
                m_maxLevel = 1,
                m_minLevel = 1,
                m_levelUpMinCenterDistance = 0f,
                m_overrideLevelupChance = 0f,
                m_foldout = false
            };
        }

    }
    public class Critter
    {
        public Critter(string creatureName, AssetBundle bundle)
        {
            if (bundle.LoadAsset<GameObject>(creatureName) is { } prefab)
            {
                Prefab = prefab;
                GetData();
                m_critters[creatureName] = this;
            }
            else
            {
                Debug.LogWarning(creatureName + " is null");
            }
        }
        public readonly bool Enabled = true;
        public readonly GameObject Prefab = null!;
        public Heightmap.Biome Biome = Heightmap.Biome.None;
        public Heightmap.BiomeArea BiomeArea = Heightmap.BiomeArea.Everything;
        public int MaxSpawn = 1;
        public float SpawnInterval = 100f;
        public float SpawnDistance = 50f;
        public float SpawnRadiusMin = 10f;
        public float SpawnRadiusMax = 100f;
        public TimeOfDay TimeOfDay = TimeOfDay.Both;
        public string RequiredGlobalKey = "";
        public List<string> RequiredEnvironments = new();
        public string Group = "DeepNorth";
        public int GroupSizeMin = 0;
        public int GroupSizeMax = 1;
        public float GroupRadius = 50f;
        public float MinAltitude = -1000f;
        public float MaxAltitude = 1000f;
        public float MinTilt = 0f;
        public float MaxTilt = 50f;
        public bool HuntPlayer = false;
        public float GroundOffset = 0.5f;
        public int MaxLevel = 3;
        public int MinLevel = 1;
        public float LevelUpMinCenterDistance = 1f;
        public float OverrideLevelUpChance = 0f;
        public readonly string CloneAllEffectsFrom = "";
        public readonly List<string> RandomWeapons = new();
        public readonly List<string> RandomShields = new();
        public readonly List<string> DefaultItems = new();
        public string CloneAnimatorFrom = "";
        public Character.Faction Faction = Character.Faction.Players;
        public bool Boss;
        public string BossEvent = "";
        public float Health;
        public readonly List<string> HitEffects = new();
        public readonly List<string> DeathEffects = new();
        public readonly List<string> JumpEffects = new();
        public readonly List<string> ConsumeEffects = new();
        public readonly List<string> WaterEffects = new();
        public readonly List<string> EquipEffects = new();
        public readonly List<string> CritEffects = new();
        public readonly List<string> BackStabEffects = new();
        public readonly string SpawnMessage = "";
        public readonly string DeathMessage = "";
        public readonly List<string> AlertedEffects = new();
        public readonly List<string> IdleSounds = new();
        public readonly List<string> ConsumeItems = new();
        public string FootStepEffectsFrom = "Fenring";
        public string DyingTrigger = "";
        public float OnDeathDelay;
        public EffectList? OnDeathEffects;
        public readonly Dictionary<string, string> m_enableVisuals = new();
        public readonly List<string> TamedEffects = new();
        public readonly List<string> SootheEffects = new();
        public readonly List<string> PetEffects = new();
        public readonly List<string> UnSummonEffects = new();
        public readonly CritterConfigs configs = new();
        public StatusEffects.Critter? CreatureStatusEffect;
        public readonly List<Attack> Attacks = new();
        public readonly CritterDrops Drops = new();

        public void SetOnDeathTrigger(string trigger, float delay)
        {
            DyingTrigger = trigger;
            OnDeathDelay = delay;
            OnDeath.m_delayedDeathCritters[Prefab.name] = this;
        }

        public void AddEnableVisualTrigger(string sharedName, string childName)
        {
            m_enableVisuals[sharedName] = childName;
            EnableVisual.m_enableVisualCritters[Prefab.name] = this;
        }
        
        public void SetupConfigs()
        {
            var englishName = Localization.instance.Localize($"${Name.Key}");
            configs.FactionConfig = BestiaryPlugin._Plugin.config(englishName, "0. Faction", Faction, "Set character faction");
            configs.HealthConfig = BestiaryPlugin._Plugin.config(englishName, "1. Health", Health, "Set health of creature");
            configs.DamageConfig = BestiaryPlugin._Plugin.config(englishName, "2. Damage Output", 1f, new ConfigDescription("Set damage output multiplier, 0.0 deals no damage", new AcceptableValueRange<float>(0f, 10f)));
            configs.ArmorConfig = BestiaryPlugin._Plugin.config(englishName, "3. Damage Received", 1f, new ConfigDescription("Set the damage received multiplier, 0% makes creature invulnerable, 100% takes normal damage", new AcceptableValueRange<float>(0f, 1f)));
            configs.BiomeConfig = BestiaryPlugin._Plugin.config(englishName, "4. Biome", Biome, "Set available biomes for creature spawn in");
            configs.IntervalConfig = BestiaryPlugin._Plugin.config(englishName, "5. Spawn Interval", SpawnInterval, "Set interval between spawns in a zone");
            configs.MaxSpawnConfig = BestiaryPlugin._Plugin.config(englishName, "6. Max Spawned", MaxSpawn, "Set the max amount of spawn per zone");
            configs.SpawnTimeConfig = BestiaryPlugin._Plugin.config(englishName, "7. Spawn Time", TimeOfDay, "Set time of day creature can spawn");

            string FormatDropData()
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (var index = 0; index < Drops.m_dropData.Count; index++)
                {
                    var drop = Drops.m_dropData[index];
                    stringBuilder.AppendFormat("{0}:{1}:{2}:{3}", drop.m_prefabName, drop.m_min, drop.m_max, drop.m_chance);
                    if (index >= Drops.m_dropData.Count - 1) continue;
                    stringBuilder.Append(",");
                }

                return stringBuilder.ToString();
            }

            configs.DropConfig = BestiaryPlugin._Plugin.config(englishName, "8. Drops", FormatDropData(), "Define drop list, [prefabName]:[min]:[max]:[chance], seperated by commas");
            
            if (Prefab.GetComponent<Tameable>() && Prefab.TryGetComponent(out MonsterAI monsterAI))
            {
                string FormatConsumeItems()
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    for (var index = 0; index < ConsumeItems.Count; index++)
                    {
                        var item = ConsumeItems[index];
                        stringBuilder.Append(item);
                        if (index >= monsterAI.m_consumeItems.Count - 1) continue;
                        stringBuilder.Append(",");
                    }

                    return stringBuilder.ToString();
                }
                configs.ConsumeConfig = BestiaryPlugin._Plugin.config(englishName, "8. Consume Items", FormatConsumeItems(), "Define consumable items by creature, [prefabName], seperated by a comma");
            }
        }
        
        public Attack SetAttack(string attackName)
        {
            Attack attack = new Attack(attackName);
            Attacks.Add(attack);
            return attack;
        }
        
        private void GetData()
        {
            if (Prefab.TryGetComponent(out Character character))
            {
                Faction = character.m_faction;
                Health = character.m_health;
                BossEvent = character.m_bossEvent;
                Boss = character.m_boss;
            }
        }

        public void Setup()
        {
            CloneAllFrom();
            EditHumanoid();
            EditMonsterAI();
            EditTame();
            CloneFootSteps();
            CloneAnimator();
        }
        
        private void CloneAllFrom()
        {
            if (CloneAllEffectsFrom.IsNullOrWhiteSpace() || !ZNetScene.instance) return;
            if (ZNetScene.instance.GetPrefab(CloneAllEffectsFrom) is not { } original) return;
            if (!original.TryGetComponent(out Humanoid fromHumanoid) || !Prefab.TryGetComponent(out Humanoid toHumanoid)) return;
            toHumanoid.m_defaultItems = fromHumanoid.m_defaultItems;
            toHumanoid.m_randomShield = fromHumanoid.m_randomShield;
            toHumanoid.m_randomArmor = fromHumanoid.m_randomArmor;
            toHumanoid.m_hitEffects = fromHumanoid.m_hitEffects;
            toHumanoid.m_critHitEffects = fromHumanoid.m_critHitEffects;
            toHumanoid.m_backstabHitEffects = fromHumanoid.m_backstabHitEffects;
            toHumanoid.m_deathEffects = fromHumanoid.m_deathEffects;
            toHumanoid.m_waterEffects = fromHumanoid.m_waterEffects;
            if (!original.TryGetComponent(out MonsterAI fromMonsterAI) || !Prefab.TryGetComponent(out MonsterAI toMonsterAI)) return;
            toMonsterAI.m_alertedEffects = fromMonsterAI.m_alertedEffects;
            toMonsterAI.m_idleSound = fromMonsterAI.m_idleSound;
            if (!original.TryGetComponent(out FootStep fromFootSteps) || !Prefab.TryGetComponent(out FootStep toFootSteps)) return;
            toFootSteps.m_effects = fromFootSteps.m_effects;
        }
        
        private void EditHumanoid()
        {
            if (!Prefab.TryGetComponent(out Humanoid humanoid)) return;
            humanoid.m_defeatSetGlobalKey = $"defeated_{Prefab.name.ToLower()}";
            humanoid.m_health = configs.HealthConfig.Value;
            humanoid.m_faction = configs.FactionConfig.Value;
            humanoid.m_boss = Boss;
            humanoid.m_bossEvent = BossEvent;
            humanoid.m_group = Group;
            RegisterEffectList(humanoid.m_hitEffects);
            RegisterEffectList(humanoid.m_critHitEffects);
            RegisterEffectList(humanoid.m_backstabHitEffects);
            RegisterEffectList(humanoid.m_deathEffects);
            RegisterEffectList(humanoid.m_waterEffects);
            RegisterEffectList(humanoid.m_tarEffects);
            RegisterEffectList(humanoid.m_slideEffects);
            RegisterEffectList(humanoid.m_jumpEffects);
            RegisterEffectList(humanoid.m_flyingContinuousEffect);
            RegisterEffectList(humanoid.m_pheromoneLoveEffect);
            FaunaManager.RegisterItems(humanoid.m_defaultItems);
            FaunaManager.RegisterItems(humanoid.m_randomWeapon);
            FaunaManager.RegisterItems(humanoid.m_randomArmor);
            UpdateEffectList(HitEffects, ref humanoid.m_hitEffects);
            UpdateEffectList(DeathEffects, ref humanoid.m_deathEffects);
            UpdateEffectList(JumpEffects, ref humanoid.m_jumpEffects);
            UpdateEffectList(ConsumeEffects, ref humanoid.m_consumeItemEffects);
            UpdateEffectList(WaterEffects, ref humanoid.m_waterEffects);
            UpdateEffectList(EquipEffects, ref humanoid.m_equipEffects);
            UpdateEffectList(CritEffects, ref humanoid.m_critHitEffects);
            UpdateEffectList(BackStabEffects, ref humanoid.m_backstabHitEffects);
            UpdateItems(DefaultItems, ref humanoid.m_defaultItems);
            UpdateItems(RandomWeapons, ref humanoid.m_randomWeapon);
            UpdateItems(RandomShields, ref humanoid.m_randomShield);
            EditAttackItems(humanoid);

            void SettingChanged()
            {
                humanoid.m_faction = configs.FactionConfig.Value;
                humanoid.m_health = configs.HealthConfig.Value;
            }

            configs.FactionConfig.SettingChanged += (_, _) => SettingChanged();
            configs.HealthConfig.SettingChanged += (_, _) => SettingChanged();
        }
        
        private void EditAttackItems(Humanoid humanoid)
        {
            foreach (Attack? attack in Attacks)
            {
                if (humanoid.m_defaultItems.FirstOrDefault(x => x.name == attack.m_prefabName) is not { } prefab || !prefab.TryGetComponent(out ItemDrop component)) continue;
                UpdateEffectList(attack.TriggerEffects, ref component.m_itemData.m_shared.m_triggerEffect);
                UpdateEffectList(attack.TrailStartEffects, ref component.m_itemData.m_shared.m_attack.m_trailStartEffect);
                UpdateEffectList(attack.StartEffects, ref component.m_itemData.m_shared.m_startEffect);
                UpdateEffectList(attack.HitEffects, ref component.m_itemData.m_shared.m_hitEffect);
                UpdateEffectList(attack.HitTerrainEffects, ref component.m_itemData.m_shared.m_hitTerrainEffect);
                UpdateEffectList(attack.BlockEffects, ref component.m_itemData.m_shared.m_blockEffect);
                if (!attack.GetProjectileFrom.IsNullOrWhiteSpace() && ZNetScene.instance.GetPrefab(attack.GetProjectileFrom) is { } projectile)
                {
                    component.m_itemData.m_shared.m_attack.m_attackProjectile = projectile;
                }
            }
        }
        
        private void EditMonsterAI()
        {
            if (!Prefab.TryGetComponent(out MonsterAI monsterAI)) return;
            monsterAI.m_spawnMessage = SpawnMessage;
            monsterAI.m_deathMessage = DeathMessage;
            RegisterEffectList(monsterAI.m_idleSound);
            RegisterEffectList(monsterAI.m_alertedEffects);
            RegisterEffectList(monsterAI.m_wakeupEffects);
            UpdateEffectList(IdleSounds, ref monsterAI.m_idleSound);
            UpdateEffectList(AlertedEffects, ref monsterAI.m_alertedEffects);
        }
        
        private void EditTame()
        {
            if (!Prefab.TryGetComponent(out Tameable component)) return;
            UpdateEffectList(TamedEffects, ref component.m_tamedEffect);
            UpdateEffectList(SootheEffects, ref component.m_sootheEffect);
            UpdateEffectList(PetEffects, ref component.m_petEffect);
        }
        
        private void CloneFootSteps()
        {
            if (!Prefab) return;
            if (FootStepEffectsFrom.IsNullOrWhiteSpace()) return;
            if (!Prefab.TryGetComponent(out FootStep footStep)) return;
            if (ZNetScene.instance.GetPrefab(FootStepEffectsFrom) is not {} original) return;
            if (!original.TryGetComponent(out FootStep component)) return;
            footStep.m_effects = component.m_effects;
        }
        
        private void CloneAnimator()
        {
            if (CloneAnimatorFrom.IsNullOrWhiteSpace()) return;
            if (ZNetScene.instance.GetPrefab(CloneAnimatorFrom) is not {} prefab) return;
            Animator? component = prefab.GetComponentInChildren<Animator>();
            if (component is null) return;
            Prefab.GetComponentInChildren<Animator>().runtimeAnimatorController = component.runtimeAnimatorController;
        }
        
        public void AddConsumeItems()
        {
            if (!Prefab || !Prefab.TryGetComponent(out MonsterAI monsterAI) || !Prefab.GetComponent<Tameable>()) return;
            if (configs.ConsumeConfig is null) return;
            monsterAI.m_consumeItems = configs.GetConsumeItems();
            configs.ConsumeConfig.SettingChanged += (_, _) => {monsterAI.m_consumeItems = configs.GetConsumeItems(); };
        }

        public void RegisterItems()
        {
            if (!Prefab.TryGetComponent(out Humanoid component)) return;
            RegisterItemsToDB(component.m_defaultItems);
            RegisterItemsToDB(component.m_randomWeapon);
            RegisterItemsToDB(component.m_randomShield);
        }

        public void RegisterStatusEffect()
        {
            StatusEffects.Critter effect = ScriptableObject.CreateInstance<StatusEffects.Critter>();
            effect.m_data = this;
            effect.name = $"SE_{Prefab.name}";
            if (!ObjectDB.instance.m_StatusEffects.Contains(effect)) ObjectDB.instance.m_StatusEffects.Add(effect);
            CreatureStatusEffect = effect;
        }
        
        public void AddCharacterDrops()
        {
            if (!Prefab.TryGetComponent(out CharacterDrop component)) return;
            if (Drops.m_dropData.Count == 0) return;
            component.m_drops.AddRange(configs.GetDrops());
            configs.DropConfig.SettingChanged += (_, _) => component.m_drops = configs.GetDrops();
        }

        public SpawnSystem.SpawnData GetSpawnData()
        {
            return new()
            {
                m_name = Prefab.name,
                m_enabled = Enabled,
                m_prefab = Prefab,
                m_biome = configs.BiomeConfig.Value,
                m_biomeArea = BiomeArea,
                m_maxSpawned = configs.MaxSpawnConfig.Value,
                m_spawnInterval = configs.IntervalConfig.Value,
                m_spawnDistance = SpawnDistance,
                m_spawnRadiusMin = SpawnRadiusMin,
                m_spawnRadiusMax = SpawnRadiusMax,
                m_requiredGlobalKey = RequiredGlobalKey,
                m_requiredEnvironments = RequiredEnvironments,
                m_groupSizeMin = GroupSizeMin,
                m_groupSizeMax = GroupSizeMax,
                m_groupRadius = GroupRadius,
                m_spawnAtNight = configs.SpawnTimeConfig.Value is TimeOfDay.Both or TimeOfDay.Night,
                m_spawnAtDay = configs.SpawnTimeConfig.Value is TimeOfDay.Both or TimeOfDay.Day,
                m_minAltitude = MinAltitude,
                m_maxAltitude = MaxAltitude,
                m_minTilt = MinTilt,
                m_maxTilt = MaxTilt,
                m_huntPlayer = HuntPlayer,
                m_groundOffset = GroundOffset,
                m_maxLevel = MaxLevel,
                m_minLevel = MinLevel,
                m_levelUpMinCenterDistance = LevelUpMinCenterDistance,
                m_overrideLevelupChance = OverrideLevelUpChance,
                m_foldout = false
            };
        }
        public class CritterConfigs
        {
            public ConfigEntry<Character.Faction> FactionConfig = null!;
            public ConfigEntry<float> HealthConfig = null!;
            public ConfigEntry<float> DamageConfig = null!;
            public ConfigEntry<float> ArmorConfig = null!;
            public ConfigEntry<Heightmap.Biome> BiomeConfig = null!;
            public ConfigEntry<float> IntervalConfig = null!;
            public ConfigEntry<int> MaxSpawnConfig = null!;
            public ConfigEntry<TimeOfDay> SpawnTimeConfig = null!;
            public ConfigEntry<string> DropConfig = null!;
            public List<CharacterDrop.Drop> GetDrops()
            {
                List<CharacterDrop.Drop> data = new();
                foreach (var dropInfo in DropConfig.Value.Split(','))
                {
                    var info = dropInfo.Split(':');
                    if (info.Length < 4) continue;
                    if (ObjectDB.instance.GetItemPrefab(info[0]) is { } prefab)
                    {
                        data.Add(new CharacterDrop.Drop()
                        {
                            m_prefab = prefab,
                            m_amountMin = int.TryParse(info[1], out int min) ? min : 1,
                            m_amountMax = int.TryParse(info[2], out int max) ? max : 1,
                            m_chance = float.TryParse(info[3], out float chance) ? chance : 1f,
                        });
                    }
                }
                return data;
            }

            public ConfigEntry<string>? ConsumeConfig;

            public List<ItemDrop> GetConsumeItems()
            {
                List<ItemDrop> output = new();
                if (ConsumeConfig is null) return output;
                foreach (var itemName in ConsumeConfig.Value.Split(','))
                {
                    if (ObjectDB.instance.GetItemPrefab(itemName) is { } prefab && prefab.TryGetComponent(out ItemDrop component))
                    {
                        output.Add(component);
                    }
                }

                return output;
            }
        }
        public class Attack
        {
            public readonly string m_prefabName;
            public readonly List<string> TriggerEffects = new();
            public readonly List<string> TrailStartEffects = new();
            public readonly List<string> StartEffects = new();
            public readonly List<string> HitEffects = new();
            public readonly List<string> HitTerrainEffects = new();
            public readonly List<string> BlockEffects = new();
            public string GetProjectileFrom = "";

            public Attack(string prefabName)
            {
                m_prefabName = prefabName;
            }
        }
        
        private LocalizeKey? _name;
    
        public LocalizeKey Name
        {
            get
            {
                if (_name is { } name)
                {
                    return name;
                }
    
                Character data = Prefab.GetComponent<Character>();
                if (data.m_name.StartsWith("$"))
                {
                    _name = new LocalizeKey(data.m_name);
                }
                else
                {
                    string key = "$enemy_" + Prefab.name.Replace(" ", "_");
                    _name = new LocalizeKey(key).English(data.m_name);
                    data.m_name = key;
                }
                return _name;
            }
        }
    }
    
    public class CritterDrops
    {
        public readonly List<DropData> m_dropData = new();

        public void Add(string prefabName, int min, int max, float chance)
        {
            DropData data = new DropData()
            {
                m_prefabName = prefabName,
                m_min = min,
                m_max = max,
                m_chance = chance,
            };
            m_dropData.Add(data);
        }
            
        public class DropData
        {
            public string m_prefabName = null!;
            public int m_min;
            public int m_max;
            public float m_chance;
        }
    }
}

    