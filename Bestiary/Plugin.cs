using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Bestiary.Behaviors;
using Bestiary.Managers;
using Bestiary.StatusEffects;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using LocalizationManager;
using Managers;
using ServerSync;
using UnityEngine;

namespace Bestiary
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class BestiaryPlugin : BaseUnityPlugin
    {
        internal const string ModName = "Bestiary";
        internal const string ModVersion = "0.0.1";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private static readonly string ConfigFileName = ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource BestiaryLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        private enum Toggle { On = 1, Off = 0 }

        public static GameObject _Root = null!;
        public static BestiaryPlugin _Plugin = null!;
        
        private static readonly AssetBundle _Assets = GetAssetBundle("beastiarybundle");
        
        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private static AssetBundle GetAssetBundle(string fileName)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
            using Stream? stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }
        public void Awake()
        {
            Localizer.Load();
            _Plugin = this;
            _Root = new GameObject("root");
            _Root.SetActive(false);
            DontDestroyOnLoad(_Root);
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            SetupCreatures();
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            // SetupWatcher();
        }

        private void SetupCreatures()
        {
            LoadWendigo();
            LoadKrampus();
            LoadLizardFish();
            LoadCatFish();
            LoadCreeper();
            LoadButterfly();
            LoadFairy();
            LoadNorthTroll();
            LoadWereBoar();
            LoadYeti();
            LoadBahomet();
            LoadRatKing();
            LoadRat();
            LoadWereWolf();
            LoadSkeletons();
            LoadHatchling();
            LoadTrophies();
            LoadMaterials();
            LoadWeapons();
            LoadSquid();
        }

        private void LoadWeapons()
        {
            Item LokiStaff = new(_Assets, "LokiStaff_RS");
            LokiStaff.Name.English("Loki's Stick");
            LokiStaff.Description.English("A gnarled wooden staff, crackling with Niflheim’s frost, mischief, and shadowed whispers.");
            LokiStaff.HitEffects.Add("vfx_arrowhit");
            LokiStaff.HitEffects.Add("sfx_spear_hit");
            LokiStaff.HitEffects.Add("fx_hit_camshake");
            LokiStaff.BlockEffects.Add("sfx_wood_blocked");
            LokiStaff.BlockEffects.Add("vfx_blocked");
            LokiStaff.BlockEffects.Add("fx_hit_camshake");
            LokiStaff.TriggerEffects.Add("fx_swing_camshake");
            LokiStaff.TrailStartEffects.Add("sfx_atgeir_attack");

            Item LichSword = new Item(_Assets, "SwordLichKing_RS");
            LichSword.Name.English("Lich King Sword");
            LichSword.HitEffects.Add("sfx_sword_hit");
            LichSword.HitEffects.Add("fx_hit_camshake");
            LichSword.HitEffects.Add("vfx_HitSparks");
            LichSword.BlockEffects.Add("sfx_metal_blocked");
            LichSword.BlockEffects.Add("vfx_blocked");
            LichSword.BlockEffects.Add("fx_block_camshake");
            LichSword.TriggerEffects.Add("fx_swing_camshake");
            LichSword.TrailStartEffects.Add("sfx_sword_swing");

            Item Navi = new Item(_Assets, "Navi_RS");
            Navi.Name.English("Naví");
            Navi.Description.English("Born of Freyja’s light, this sprite flits between realms, leaving enchantment in its wake.");
            Navi.Configurable = Configurability.Disabled;
            SE_Fairy NaviSE = ScriptableObject.CreateInstance<SE_Fairy>();
            NaviSE.name = nameof(NaviSE);
            NaviSE.m_name = "$" + Navi.Name.Key;
            NaviSE.m_tooltip = "$item_demister_description";
            NaviSE.m_ballPrefab = FaunaManager.RegisterAssetToZNetScene("Navi_Ball_RS", _Assets);
            NaviSE.m_icon = Navi.Prefab.GetComponent<ItemDrop>().m_itemData.GetIcon();
            NaviSE.m_maxDistance = 6f;
            NaviSE.m_ballAcceleration = 7f;
            NaviSE.m_ballMaxSpeed = 80f;
            NaviSE.m_ballFriction = 0.03f;
            NaviSE.m_noiseDistance = 1.5f;
            NaviSE.m_noiseDistanceInterior = 0.2f;
            NaviSE.m_noiseDistanceYScale = 0.4f;
            NaviSE.m_noiseSpeed = 0.5f;
            NaviSE.m_characterVelocityFactor = 3f;
            NaviSE.m_rotationSpeed = 8f;
            NaviSE.m_offset = new Vector3(0.1f, 4.5f, 0.1f);
            NaviSE.m_offsetInterior = new Vector3(0.5f, 2.1f, 0f);
            NaviSE.MaxFallSpeed = config("Navi", "Max Fall Speed", 5f, new ConfigDescription("Set max fall speed", new AcceptableValueRange<float>(0f, 100f)));
            NaviSE.FallDamageModifier = config("Navi", "Fall Damage", 0f, new ConfigDescription("Set fall damage modifier", new AcceptableValueRange<float>(0f, 1f)));
            NaviSE.JumpModifier = config("Navi", "Jump Modifier", 0.1f, new ConfigDescription("Set jump altitude modifier", new AcceptableValueRange<float>(0f, 1f)));
            NaviSE.JumpStaminaUseModifier = config("Navi", "Jump Stamina Use", -0.1f, new ConfigDescription("Set jump stamina use modifier", new AcceptableValueRange<float>(-1f, 1f)));
            Navi.AddEquipStatusEffect(NaviSE);
        }
        

        private void LoadMaterials()
        {
            Item Tusk = new Item(_Assets, "Tusk_RS");
            Tusk.Name.English("Tusk");
            Tusk.Description.English("Exuding faint traces of primal energy and resilience");
            Tusk.Configurable = Configurability.Disabled;

            Item RatKingSpine = new Item(_Assets, "RatKingSpine_RS");
            RatKingSpine.Name.English("Rátskarl Needle");
            RatKingSpine.Description.English("");
            RatKingSpine.Configurable = Configurability.Disabled;
        }

        private void LoadTrophies()
        {
            Item TrophyRatKing = new Item(_Assets, "TrophyRatKing_RS");
            TrophyRatKing.Name.English("Rátskarl Trophy");
            TrophyRatKing.Configurable = Configurability.Disabled;

            Item TrophyRat = new Item(_Assets, "TrophyRat_RS");
            TrophyRat.Name.English("Rat Trophy");
            TrophyRat.Configurable = Configurability.Disabled;
            
            Item TrophyCatFish = new Item(_Assets, "TrophyCatFish_RS");
            TrophyCatFish.Name.English("Sildrak Trophy");
            TrophyCatFish.Configurable = Configurability.Disabled;
            
            Item TrophySquid = new Item(_Assets, "TrophySquid_RS");
            TrophySquid.Name.English("Akkar Trophy");
            TrophySquid.Configurable = Configurability.Disabled;

            Item TrophyWereWolf = new Item(_Assets, "TrophyWereWolf_RS");
            TrophyWereWolf.Name.English("Were-wolf Trophy");
            TrophyWereWolf.Configurable = Configurability.Disabled;

            Item TrophySkeletonWarlord = new Item(_Assets, "TrophySkeletonWarlord_RS");
            TrophySkeletonWarlord.Name.English("Warlord Trophy");
            TrophySkeletonWarlord.Configurable = Configurability.Disabled;
            
            Item TrophySkeletonMage = new Item(_Assets, "TrophySkeletonMage_RS");
            TrophySkeletonMage.Name.English("Mage Trophy");
            TrophySkeletonMage.Configurable = Configurability.Disabled;
            
            Item TrophyBahomet = new Item(_Assets, "TrophyBahomet_RS");
            TrophyBahomet.Name.English("Bahomet Trophy");
            TrophyBahomet.Configurable = Configurability.Disabled;
            
            Item TrophyYeti = new(_Assets, "TrophyYeti_RS");
            TrophyYeti.Name.English("Yeti Trophy");
            TrophyYeti.Configurable = Configurability.Disabled;
        
            Item TrophyWereBoar = new(_Assets, "TrophyWereBoar_RS");
            TrophyWereBoar.Name.English("Grimsvin Trophy");
            TrophyWereBoar.Configurable = Configurability.Disabled;
        
            Item TrophyNorthTroll = new Item(_Assets, "TrophyNorthTroll_RS");
            TrophyNorthTroll.Name.English("Jotunn Trophy");
            TrophyNorthTroll.Configurable = Configurability.Disabled;
        
            Item TrophyWendigo = new(_Assets, "TrophyWendigo_RS");
            TrophyWendigo.Name.English("Wendigo Trophy");
            TrophyWendigo.Configurable = Configurability.Disabled;
        
            Item TrophyLoki = new Item(_Assets, "TrophyLoki_RS");
            TrophyLoki.Name.English("Loki Trophy");
            TrophyLoki.Configurable = Configurability.Disabled;
        
            Item TrophyCreeper = new Item(_Assets, "TrophyCreeper_RS");
            TrophyCreeper.Name.English("Myrvarg Trophy");
            TrophyCreeper.Configurable = Configurability.Disabled;
        
            Item TrophyLizardFish = new Item(_Assets, "TrophyLizardFish_RS");
            TrophyLizardFish.Name.English("Skaldrik Trophy");
            TrophyLizardFish.Configurable = Configurability.Disabled;
        }

        private void LoadSkeletons()
        {
            FaunaManager.ProjectileData SkeletonArrowProjectileInfinite = new FaunaManager.ProjectileData("skeleton_archer_bow_projectile", _Assets);
            SkeletonArrowProjectileInfinite.HitEffects.Add("sfx_arrow_hit");
            SkeletonArrowProjectileInfinite.HitEffects.Add("vfx_arrowhit");

            FaunaManager.RegisterAssetToZNetScene("fx_skeleton_mage_hit", _Assets);
            FaunaManager.RegisterAssetToZNetScene("fx_skeleton_mage_splinter_hit", _Assets);
            FaunaManager.ProjectileData SkeletonMageProjectile = new FaunaManager.ProjectileData("staff_skeleton_mage_projectile", _Assets);
            FaunaManager.ProjectileData SkeletonMageSplinterProjectile = new FaunaManager.ProjectileData("staff_skeleton_mage_splinter_projectile", _Assets);

            FaunaManager.Critter SkeletonMage = new FaunaManager.Critter("RS_Skeleton_Mage", _Assets);
            SkeletonMage.Name.English("Skeleton Mage");
            SkeletonMage.DefaultItems.Add("StaffSkeletonMage_RS");
            SkeletonMage.DefaultItems.Add("StaffClusterbomb");
            SkeletonMage.Drops.Add("TrophySkeletonMage_RS", 1, 1, 0.3f);
            SkeletonMage.Drops.Add("RS_AlchemicEssence", 1, 1, 0.5f);
            SkeletonMage.Drops.Add("StaffSkeletonMage_RS", 1, 1, 0.05f);
            SkeletonMage.HitEffects.Add("vfx_skeleton_hit");
            SkeletonMage.HitEffects.Add("sfx_skeleton_hit");
            SkeletonMage.CritEffects.Add("fx_crit");
            SkeletonMage.BackStabEffects.Add("fx_backstab");
            SkeletonMage.DeathEffects.Add("vfx_skeleton_death");
            SkeletonMage.DeathEffects.Add("sfx_skeleton_big_death");
            SkeletonMage.WaterEffects.Add("vfx_water_surface");
            SkeletonMage.FootStepEffectsFrom ="Skeleton";
            SkeletonMage.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonMage.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonMage.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonMage.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonMage.CloneAnimatorFrom = "Player";
            SkeletonHumanoid.Register(SkeletonMage.Prefab.name);
            
            FaunaManager.Critter Skeleton = new FaunaManager.Critter("RS_Skeleton", _Assets);
            Skeleton.Name.English("Skeleton Brute");
            Skeleton.Drops.Add("MorningStar_RS", 1,1, 0.05f);
            Skeleton.Drops.Add("RS_AlchemicEssence", 1, 1, 0.3f);
            Skeleton.Drops.Add("DaneSkeleton_RS", 1, 1, 0.05f);
            Skeleton.Drops.Add("SledgeSkeleton_RS", 1, 1, 0.05f);
            Skeleton.Drops.Add("AxeAncient_RS", 1, 1, 0.05f);
            Skeleton.RandomWeapons.Add("MorningStar_RS");
            Skeleton.RandomWeapons.Add("AxeAncient_RS");
            Skeleton.RandomWeapons.Add("DaneBattleaxe_RS");
            Skeleton.RandomWeapons.Add("SledgeSkeleton_RS");
            Skeleton.RandomWeapons.Add("AxeBerzerkr");
            Skeleton.DefaultItems.Add("ShieldIronBuckler");
            Skeleton.HitEffects.Add("vfx_skeleton_hit");
            Skeleton.HitEffects.Add("sfx_skeleton_hit");
            Skeleton.CritEffects.Add("fx_crit");
            Skeleton.BackStabEffects.Add("fx_backstab");
            Skeleton.DeathEffects.Add("vfx_skeleton_death");
            Skeleton.DeathEffects.Add("sfx_skeleton_big_death");
            Skeleton.WaterEffects.Add("vfx_water_surface");
            Skeleton.FootStepEffectsFrom = "Skeleton";
            Skeleton.AlertedEffects.Add("sfx_skeleton_alerted");
            Skeleton.IdleSounds.Add("sfx_skeleton_idle");
            Skeleton.AlertedEffects.Add("sfx_skeleton_alerted");
            Skeleton.IdleSounds.Add("sfx_skeleton_idle");
            Skeleton.CloneAnimatorFrom = "Player";
            SkeletonHumanoid.Register(Skeleton.Prefab.name);

            FaunaManager.Critter SkeletonArcher = new FaunaManager.Critter("RS_Skeleton_Archer", _Assets);
            SkeletonArcher.Name.English("Skeleton Archer");
            SkeletonArcher.Drops.Add("BowSkeleton_RS", 1, 1, 0.05f);
            SkeletonArcher.Drops.Add("ArrowForgotten_RS", 1, 20, 0.05f);
            SkeletonArcher.Drops.Add("ArrowSkeleton_RS", 1, 20, 0.05f);
            SkeletonArcher.Drops.Add("RS_AlchemicEssence", 1, 1, 0.3f);
            SkeletonArcher.Drops.Add("CrossBowSkeleton_RS", 1, 1, 0.05f);
            SkeletonArcher.DefaultItems.Add("BowSkeletonInfinite_RS");
            SkeletonArcher.DefaultItems.Add("BowAshlands");
            SkeletonArcher.HitEffects.Add("vfx_skeleton_hit");
            SkeletonArcher.HitEffects.Add("sfx_skeleton_hit");
            SkeletonArcher.CritEffects.Add("fx_crit");
            SkeletonArcher.BackStabEffects.Add("fx_backstab");
            SkeletonArcher.DeathEffects.Add("vfx_skeleton_death");
            SkeletonArcher.DeathEffects.Add("sfx_skeleton_big_death");
            SkeletonArcher.WaterEffects.Add("vfx_water_surface");
            SkeletonArcher.FootStepEffectsFrom = "Skeleton";
            SkeletonArcher.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonArcher.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonArcher.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonArcher.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonArcher.CloneAnimatorFrom = "Player";
            SkeletonHumanoid.Register(SkeletonArcher.Prefab.name);

            FaunaManager.Critter SkeletonRogue = new FaunaManager.Critter("RS_Skeleton_Rogue", _Assets);
            SkeletonRogue.Name.English("Skeleton Rogue");
            SkeletonRogue.Drops.Add("KnifeSkeletonRogue_RS", 1, 1, 0.05f);
            SkeletonRogue.Drops.Add("RS_AlchemicEssence", 1, 1, 0.3f);
            SkeletonRogue.DefaultItems.Add("KnifeSkeletonRogue_RS");
            SkeletonRogue.DefaultItems.Add("KnifeBlackMetal");
            SkeletonRogue.HitEffects.Add("vfx_skeleton_hit");
            SkeletonRogue.HitEffects.Add("sfx_skeleton_hit");
            SkeletonRogue.CritEffects.Add("fx_crit");
            SkeletonRogue.BackStabEffects.Add("fx_backstab");
            SkeletonRogue.DeathEffects.Add("vfx_skeleton_death");
            SkeletonRogue.DeathEffects.Add("sfx_skeleton_big_death");
            SkeletonRogue.WaterEffects.Add("vfx_water_surface");
            SkeletonRogue.FootStepEffectsFrom = "Skeleton";
            SkeletonRogue.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonRogue.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonRogue.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonRogue.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonRogue.CloneAnimatorFrom = "Player";
            SkeletonHumanoid.Register(SkeletonRogue.Prefab.name);

            FaunaManager.Critter SkeletonWarlord = new FaunaManager.Critter("RS_Skeleton_Warlord", _Assets);
            SkeletonWarlord.Name.English("Skeleton Warlord");
            SkeletonWarlord.Drops.Add("SwordSkeletonWarlord_RS", 1, 1, 0.05f);
            SkeletonWarlord.Drops.Add("RS_AlchemicEssence",1,1,0.3f);
            SkeletonWarlord.DefaultItems.Add("SwordSkeletonWarlord_RS");
            SkeletonWarlord.DefaultItems.Add("THSwordKrom");
            SkeletonWarlord.HitEffects.Add("vfx_skeleton_hit");
            SkeletonWarlord.HitEffects.Add("sfx_skeleton_hit");
            SkeletonWarlord.CritEffects.Add("fx_crit");
            SkeletonWarlord.BackStabEffects.Add("fx_backstab");
            SkeletonWarlord.DeathEffects.Add("vfx_skeleton_death");
            SkeletonWarlord.DeathEffects.Add("sfx_skeleton_big_death");
            SkeletonWarlord.WaterEffects.Add("vfx_water_surface");
            SkeletonWarlord.FootStepEffectsFrom = "Skeleton";
            SkeletonWarlord.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonWarlord.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonWarlord.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonWarlord.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonWarlord.CloneAnimatorFrom = "Player";
            SkeletonHumanoid.Register(SkeletonWarlord.Prefab.name);
            
            FaunaManager.Critter SkeletonKing = new FaunaManager.Critter("RS_Skeleton_King", _Assets);
            SkeletonKing.Name.English("Skeleton King");
            SkeletonKing.Boss = true;
            SkeletonKing.Drops.Add("RS_AlchemicEssence",1,1,0.3f);
            SkeletonKing.Drops.Add("SwordLichKing_RS", 1, 1, 1f);
            SkeletonKing.DefaultItems.Add("SwordLichKing_RS");
            SkeletonKing.DefaultItems.Add("ShieldFlametal");
            SkeletonKing.HitEffects.Add("vfx_skeleton_hit");
            SkeletonKing.HitEffects.Add("sfx_skeleton_hit");
            SkeletonKing.CritEffects.Add("fx_crit");
            SkeletonKing.BackStabEffects.Add("fx_backstab");
            SkeletonKing.DeathEffects.Add("vfx_skeleton_death");
            SkeletonKing.DeathEffects.Add("sfx_skeleton_big_death");
            SkeletonKing.WaterEffects.Add("vfx_water_surface");
            SkeletonKing.FootStepEffectsFrom = "Skeleton";
            SkeletonKing.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonKing.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonKing.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonKing.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonKing.CloneAnimatorFrom = "Player";
            SkeletonHumanoid.Register(SkeletonKing.Prefab.name);

            FaunaManager.Critter SkeletonWarrior = new FaunaManager.Critter("RS_Skeleton_Warrior", _Assets);
            SkeletonWarrior.Name.English("Skeleton Warrior");
            SkeletonWarrior.Drops.Add("SwordSkeletonWarrior_RS", 1,1,0.05f);
            SkeletonWarrior.Drops.Add("ShieldSkeletonWarrior_RS",1,1,0.05f);
            SkeletonWarrior.Drops.Add("RS_AlchemicEssence",1,1,0.3f);
            SkeletonWarrior.DefaultItems.Add("SwordSkeletonWarrior_RS");
            SkeletonWarrior.DefaultItems.Add("ShieldSkeletonWarrior_RS");
            SkeletonWarrior.DefaultItems.Add("SwordNiedhogg");
            SkeletonWarrior.HitEffects.Add("vfx_skeleton_hit");
            SkeletonWarrior.HitEffects.Add("sfx_skeleton_hit");
            SkeletonWarrior.CritEffects.Add("fx_crit");
            SkeletonWarrior.BackStabEffects.Add("fx_backstab");
            SkeletonWarrior.DeathEffects.Add("vfx_skeleton_death");
            SkeletonWarrior.DeathEffects.Add("sfx_skeleton_big_death");
            SkeletonWarrior.WaterEffects.Add("vfx_water_surface");
            SkeletonWarrior.FootStepEffectsFrom = "Skeleton";
            SkeletonWarrior.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonWarrior.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonWarrior.AlertedEffects.Add("sfx_skeleton_alerted");
            SkeletonWarrior.IdleSounds.Add("sfx_skeleton_idle");
            SkeletonWarrior.CloneAnimatorFrom = "Player";
            SkeletonHumanoid.Register(SkeletonWarrior.Prefab.name);

            List<string> materialNames = new()
            {
                "M_SK_Clothes_01_Dark",
                "M_SK_Clothes_02_Dark",
                "M_SK_Clothes_03_Dark",
                "M_SK_MagicStaff_Dark",
                "M_SK_Skeleton_Black",
                "M_SK_Skeleton_BlackBlood",
                "M_SK_Skull_Black",
                "M_SK_Skull_BlackBlood",
                "M_SK_Skull_BlackMoss",
                "MAT_HK_Sword",
                "MAT_SK_ROGUE_BLADE",
                "MAT_SK_ROGUE_CHEST",
                "MAT_SK_ROGUE_HOOD",
                "MAT_SK_ROGUE_PANTS",
                "MAT_SK_ROGUE_SHOES",
                "MAT_SK_WARLORD_CHAINS",
                "MAT_SK_WARLORD_CHEST",
                "MAT_SK_WARLORD_CLOAK",
                "MAT_SK_WARLORD_CLOTH",
                "MAT_SK_WARLORD_HELMET",
                "MAT_SK_WARLORD_PANTS",
                "MAT_SKELETON_Archer_Armor",
                "MAT_Skeleton_Archer_Arrow",
                "MAT_Skeleton_Archer_BOW",
                "MAT_Skeleton_Archer_CHEST",
                "MAT_Skeleton_Archer_fur",
                "MAT_Skeleton_Archer_HOOD",
                "MAT_SKELETON_WARRIOR_ARMOR",
                "MAT_SKELETON_WARRIOR_SHIELD",
                "MAT_SKELETON_WARRIOR_SWORD"
            };

            foreach (string materialName in materialNames)
            {
                MaterialReplacer.MaterialData mat = new MaterialReplacer.MaterialData(_Assets, materialName, MaterialReplacer.ShaderType.CustomCreature);
            }

        }
        
        private void LoadHatchling()
        {
            FaunaManager.Critter Hatchling = new("Hatchling_DeepNorth", _Assets)
            {
                Biome = Heightmap.Biome.DeepNorth,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
                GroundOffset = 20f
            };
            Hatchling.Name.English("Wyvern");
            Hatchling.Drops.Add("FreezeGland", 1, 1, 1f);
            Hatchling.HitEffects.Add("vfx_hatchling_hurt");
            Hatchling.HitEffects.Add("sfx_hatchling_hurt");
            Hatchling.CritEffects.Add("fx_crit");
            Hatchling.BackStabEffects.Add("fx_backstab");
            Hatchling.DeathEffects.Add("vfx_hatchling_death");
            Hatchling.DeathEffects.Add("sfx_hatchling_death");
            Hatchling.DeathEffects.Add("Hatchling_ragdoll");
            Hatchling.WaterEffects.Add("vfx_water_surface");
            Hatchling.AlertedEffects.Add("sfx_hatchling_alerted");
            Hatchling.IdleSounds.Add("sfx_hatchling_idle");
            Hatchling.CloneAnimatorFrom = "Hatchling";
            FaunaManager.Critter.Attack spit = Hatchling.SetAttack("hatchling_spit_cold1");
            spit.HitEffects.Add("vfx_HitSparks");
            spit.HitEffects.Add("sfx_greydwarf_attach_hit");
            spit.StartEffects.Add("sfx_hatching_coldball_start");
            spit.TriggerEffects.Add("sfx_hatching_coldball_launch");
            FaunaManager.ProjectileData wyvernProjectile = new FaunaManager.ProjectileData("wyvern_projectile", _Assets);
            wyvernProjectile.HitEffects.Add("vfx_ColdBall_Hit");
            wyvernProjectile.HitEffects.Add("sfx_hatching_coldball_explode");
            FaunaManager.RegisterAssetToZNetScene("Wyvern_fire_AOE", _Assets);
        }
        
        private void LoadBahomet()
        {
            FaunaManager.Critter Bahomet = new("Bahomet_RS", _Assets)
            {
                Biome = Heightmap.Biome.None,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            
            Bahomet.Name.English("Bahomet");
            Bahomet.Drops.Add("TrophyBahomet_RS", 1, 1, 1f);
            Bahomet.Boss = true;
            Bahomet.SetOnDeathTrigger("dying", 4.7f);
            Bahomet.HitEffects.Add("vfx_HitSparks");
            Bahomet.HitEffects.Add("sfx_fenring_claw_hit");
            Bahomet.HitEffects.Add("vfx_fenring_hurt");
            Bahomet.CritEffects.Add("fx_crit");
            Bahomet.BackStabEffects.Add("fx_backstab");
            Bahomet.DeathEffects.Add("vfx_corpse_destruction_small");
            Bahomet.DeathEffects.Add("vfx_fenring_cultist_hildir_death");
            Bahomet.WaterEffects.Add("vfx_water_surface");
            Bahomet.FootStepEffectsFrom = "Troll";
            Bahomet.AlertedEffects.Add("sfx_fenring_alerted");
            Bahomet.IdleSounds.Add("sfx_fenring_idle");
            FaunaManager.Critter.Attack frost = Bahomet.SetAttack("Bahomet_attack_frost");
            FaunaManager.Critter.Attack iceClaw = Bahomet.SetAttack("Bahomet_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble = Bahomet.SetAttack("Bahomet_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova = Bahomet.SetAttack("Bahomet_attack_IceNova");
            FaunaManager.RegisterAssetToZNetScene("fx_Bahomet_Fissure_Prespawn", _Assets);
            FaunaManager.RegisterAssetToZNetScene("Bahomet_WallOfFire_AOE", _Assets);
            foreach (var attack in Bahomet.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }
            MaterialReplacer.MaterialData BahometBody = new MaterialReplacer.MaterialData(_Assets, "Mat_Baphomet_Body_Skin1", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData BahometFur = new MaterialReplacer.MaterialData(_Assets, "Mat_Baphomet_Fur_Skin1", MaterialReplacer.ShaderType.CustomCreature);
        }

        private void LoadYeti()
        {
            FaunaManager.Critter Yeti = new("Yeti_RS", _Assets)
            {
                Biome = Heightmap.Biome.None,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            Yeti.Name.English("Yeti");
            Yeti.SetOnDeathTrigger("dying", 4.7f);
            Yeti.Drops.Add("TrophyYeti_RS", 1, 1, 1f);
            Yeti.Drops.Add("HairBundle_RS", 1, 1, 1f);
            Yeti.Drops.Add("BeastChunk_RS", 1, 2, 0.5f);
            Yeti.HitEffects.Add("vfx_HitSparks");
            Yeti.HitEffects.Add("sfx_fenring_claw_hit");
            Yeti.HitEffects.Add("vfx_fenring_hurt");
            Yeti.CritEffects.Add("fx_crit");
            Yeti.BackStabEffects.Add("fx_backstab");
            Yeti.DeathEffects.Add("vfx_corpse_destruction_small");
            Yeti.DeathEffects.Add("vfx_fenring_cultist_hildir_death");
            Yeti.WaterEffects.Add("vfx_water_surface");
            Yeti.FootStepEffectsFrom = "Fenring";
            Yeti.AlertedEffects.Add("sfx_fenring_alerted");
            Yeti.IdleSounds.Add("sfx_fenring_idle");
            FaunaManager.Critter.Attack frost = Yeti.SetAttack("Yeti_attack_frost");
            FaunaManager.Critter.Attack iceClaw = Yeti.SetAttack("Yeti_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble = Yeti.SetAttack("Yeti_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova = Yeti.SetAttack("Yeti_attack_IceNova");
            foreach (var attack in Yeti.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }

            MaterialReplacer.MaterialData YetiBody = new MaterialReplacer.MaterialData(_Assets, "Mat_Yeti_Body", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData YetiFur = new MaterialReplacer.MaterialData(_Assets, "Mat_Yeti_Fur", MaterialReplacer.ShaderType.CustomCreature);
        }

        private void LoadWereBoar()
        {
            FaunaManager.Critter WereBoar = new("WereBoar_RS", _Assets)
            {
                Biome = Heightmap.Biome.DeepNorth,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            WereBoar.Name.English("Grimsvin");
            WereBoar.SetOnDeathTrigger("dying", 4.7f);
            WereBoar.Drops.Add("TrophyWereBoar_RS", 1, 1, 1f);
            WereBoar.Drops.Add("Tusk_RS", 1, 2, 1f);
            WereBoar.Drops.Add("HairBundle_RS", 1, 1, 1f);
            WereBoar.Drops.Add("BeastMeat_RS", 1, 1, 1f);
            WereBoar.HitEffects.Add("vfx_HitSparks");
            WereBoar.HitEffects.Add("sfx_boar_hit");
            WereBoar.HitEffects.Add("vfx_boar_hit");
            WereBoar.CritEffects.Add("fx_crit");
            WereBoar.BackStabEffects.Add("fx_backstab");
            WereBoar.DeathEffects.Add("vfx_corpse_destruction_small");
            WereBoar.DeathEffects.Add("vfx_boar_death");
            WereBoar.DeathEffects.Add("sfx_boar_death");
            WereBoar.WaterEffects.Add("vfx_water_surface");
            WereBoar.FootStepEffectsFrom = "Boar";
            FaunaManager.Critter.Attack frost = WereBoar.SetAttack("WereBoar_attack_frost");
            FaunaManager.Critter.Attack iceClaw = WereBoar.SetAttack("WereBoar_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble = WereBoar.SetAttack("WereBoar_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova = WereBoar.SetAttack("WereBoar_attack_IceNova");
            foreach (var attack in WereBoar.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }

            MaterialReplacer.MaterialData WereBoarFur = new MaterialReplacer.MaterialData(_Assets, "T_Fur_Skin2_Albedo", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WereBoarSkin = new MaterialReplacer.MaterialData(_Assets, "Mat_WereBoar_Body_Skin1", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WereBoarFur1 = new MaterialReplacer.MaterialData(_Assets, "Mat_WereBoar_Fur_Skin1", MaterialReplacer.ShaderType.CustomCreature);
        }
        private void LoadRat()
        {
            FaunaManager.Critter Rat = new("Rat_RS", _Assets)
            {
                Biome = Heightmap.Biome.None,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            Rat.Name.English("Rat");
            Rat.Drops.Add("HairBundle_RS", 1, 1, 1f);
            Rat.Drops.Add("BeastChunk_RS", 1, 1, 0.5f);
            Rat.Drops.Add("TrophyRat_RS", 1, 1, 0.1f);
            Rat.SetOnDeathTrigger("dying", 4.7f);
            Rat.HitEffects.Add("vfx_HitSparks");
            Rat.HitEffects.Add("sfx_boar_hit");
            Rat.HitEffects.Add("vfx_boar_hit");
            Rat.CritEffects.Add("fx_crit");
            Rat.BackStabEffects.Add("fx_backstab");
            Rat.DeathEffects.Add("vfx_corpse_destruction_small");
            Rat.DeathEffects.Add("vfx_boar_death");
            Rat.DeathEffects.Add("sfx_boar_death");
            Rat.WaterEffects.Add("vfx_water_surface");
            Rat.FootStepEffectsFrom = "Boar";
            Rat.AlertedEffects.Add("sfx_fenring_alerted");
            Rat.AlertedEffects.Add("sfx_boar_alerted");
            FaunaManager.Critter.Attack iceClaw = Rat.SetAttack("Rat_attack_iceclaw");
            FaunaManager.Critter.Attack frost = Rat.SetAttack("Rat_attack_frost");
            foreach (var attack in Rat.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }

            MaterialReplacer.MaterialData RatBody = new MaterialReplacer.MaterialData(_Assets, "Mat_WereRat_Body_Skin3", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData RatFur = new MaterialReplacer.MaterialData(_Assets, "Mat_WereRat_Fur_Skin3", MaterialReplacer.ShaderType.CustomCreature);
        }
        private void LoadRatKing()
        {
            FaunaManager.Critter RatKing = new("RatKing_RS", _Assets)
            {
                Biome = Heightmap.Biome.None,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            RatKing.Boss = true;
            RatKing.Name.English("Rátskarl");
            RatKing.Drops.Add("TrophyRatKing_RS", 1, 1, 1f);
            RatKing.Drops.Add("RatKingSpine_RS", 3, 3, 1f);
            RatKing.SetOnDeathTrigger("dying", 4.7f);
            RatKing.HitEffects.Add("vfx_HitSparks");
            RatKing.HitEffects.Add("sfx_boar_hit");
            RatKing.HitEffects.Add("vfx_boar_hit");
            RatKing.CritEffects.Add("fx_crit");
            RatKing.BackStabEffects.Add("fx_backstab");
            RatKing.DeathEffects.Add("vfx_corpse_destruction_small");
            RatKing.DeathEffects.Add("vfx_boar_death");
            RatKing.DeathEffects.Add("sfx_boar_death");
            RatKing.WaterEffects.Add("vfx_water_surface");
            RatKing.FootStepEffectsFrom = "Boar";
            RatKing.AlertedEffects.Add("sfx_fenring_alerted");
            RatKing.AlertedEffects.Add("sfx_boar_alerted");
            FaunaManager.Critter.Attack throwProjectile = RatKing.SetAttack("RatBrute_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble = RatKing.SetAttack("RatBrute_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova = RatKing.SetAttack("RatBrute_attack_IceNova");
            FaunaManager.Critter.Attack frost = RatKing.SetAttack("RatBrute_attack_frost");
            FaunaManager.Critter.Attack taunt = RatKing.SetAttack("RatBrute_taunt");
            foreach (var attack in RatKing.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }
            FaunaManager.RegisterAssetToZNetScene("RatKing_Acid_AOE", _Assets);
            FaunaManager.RegisterAssetToZNetScene("vfx_AcidSparks", _Assets);
            FaunaManager.ProjectileData RatProjectile = new FaunaManager.ProjectileData("RatBrute_projectile", _Assets);
            RatProjectile.HitEffects.Add("sfx_troll_rock_destroyed");
            RatProjectile.HitEffects.Add("vfx_troll_rock_destroyed");
            RatProjectile.HitEffects.Add("sfx_troll_attack_hit");

            MaterialReplacer.MaterialData RatBruteBody = new MaterialReplacer.MaterialData(_Assets, "Mat_Chupacabra_Body_Skin4", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData RatBruteFur = new MaterialReplacer.MaterialData(_Assets, "Mat_Chupacabra_Fur_Skin4", MaterialReplacer.ShaderType.CustomCreature);
        }

        private void LoadWendigo()
        {
            FaunaManager.Critter Wendigo = new("Wendigo_RS", _Assets)
            {
                Biome = Heightmap.Biome.DeepNorth,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
                TimeOfDay = FaunaManager.TimeOfDay.Night
            };
            Wendigo.Name.English("Wendigo");
            Wendigo.SetOnDeathTrigger("dying", 4.7f);
            Wendigo.Drops.Add("HairBundle_RS", 1, 2, 1f);
            Wendigo.Drops.Add("BeastEye_RS", 1, 1, 0.5f);
            Wendigo.Drops.Add("BeastLeg_RS", 1, 1, 0.5f);
            Wendigo.HitEffects.Add("vfx_HitSparks");
            Wendigo.HitEffects.Add("sfx_greydwarf_attack_hit") ;
            Wendigo.DeathEffects.Add("vfx_corpse_destruction_small");
            Wendigo.DeathEffects.Add("sfx_greydwarf_death");
            Wendigo.WaterEffects.Add("vfx_water_surface");
            Wendigo.CritEffects.Add("fx_crit");
            Wendigo.BackStabEffects.Add("fx_backstab");
            Wendigo.FootStepEffectsFrom = "Fenring";
            Wendigo.AlertedEffects.Add("sfx_fenring_alerted");
            Wendigo.IdleSounds.Add("sfx_fenring_idle");
            FaunaManager.Critter.Attack frost = Wendigo.SetAttack("Wendigo_attack_frost");
            FaunaManager.Critter.Attack iceClaw = Wendigo.SetAttack("Wendigo_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble = Wendigo.SetAttack("Wendigo_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova = Wendigo.SetAttack("Wendigo_attack_IceNova");
            foreach (var attack in Wendigo.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }

            FaunaManager.Critter Wendigo1 = new("Wendigo1_RS", _Assets)
            {
                Biome = Heightmap.Biome.None,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            Wendigo1.Name.English("Wicked Wendigo");
            Wendigo1.SetOnDeathTrigger("dying", 4.7f);
            Wendigo1.Drops.Add("TrophyWendigo_RS", 1, 1, 0.3f);
            Wendigo1.Drops.Add("HairBundle_RS", 1, 2, 1f);
            Wendigo1.Drops.Add("BeastEye_RS", 1, 1, 0.5f);
            Wendigo1.Drops.Add("UncookedBeastLeg_RS", 1, 1, 0.5f);
            Wendigo1.HitEffects.Add("vfx_HitSparks");
            Wendigo1.HitEffects.Add("sfx_greydwarf_attack_hit") ;
            Wendigo1.DeathEffects.Add("vfx_corpse_destruction_small");
            Wendigo1.DeathEffects.Add("sfx_greydwarf_death");
            Wendigo1.WaterEffects.Add("vfx_water_surface");
            Wendigo1.CritEffects.Add("fx_crit");
            Wendigo1.BackStabEffects.Add("fx_backstab");
            Wendigo1.FootStepEffectsFrom = "Fenring";
            Wendigo1.AlertedEffects.Add("sfx_fenring_alerted");
            Wendigo1.IdleSounds.Add("sfx_fenring_idle");
            FaunaManager.Critter.Attack frost1 = Wendigo1.SetAttack("Wendigo_attack_frost");
            FaunaManager.Critter.Attack iceClaw1= Wendigo1.SetAttack("Wendigo_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble1 = Wendigo1.SetAttack("Wendigo_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova1 = Wendigo1.SetAttack("Wendigo_attack_IceNova");
            foreach (var attack in Wendigo1.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }

            LevelTransmission transmission = Wendigo.Prefab.AddComponent<LevelTransmission>();
            transmission.m_spawns.Add(Wendigo1.Prefab);
            transmission.SpawnEffects.Add("fx_loki_teleport");
            transmission.SpawnEffects.Add("sfx_dverger_heavyattack_launch");
            
            MaterialReplacer.MaterialData WendigoFur = new MaterialReplacer.MaterialData(_Assets, "Mat_Wendigo_fur", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WendigoBody = new MaterialReplacer.MaterialData(_Assets, "Mat_Wendigo_Body", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WendigoHead = new MaterialReplacer.MaterialData(_Assets, "Mat_Wendigo_Head2", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WendigoFur1 = new MaterialReplacer.MaterialData(_Assets, "Mat_Wendigo_fur1", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WendigoBody1 = new MaterialReplacer.MaterialData(_Assets, "Mat_Wendigo_Body1", MaterialReplacer.ShaderType.CustomCreature);
        }

        private void LoadKrampus()
        {
            FaunaManager.Critter Krampus = new("Krampus_RS", _Assets)
            {
                Biome = Heightmap.Biome.None,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            Krampus.Boss = true;
            Krampus.Name.English("Krampus");
            Krampus.SetOnDeathTrigger("dying", 4.7f);
            Krampus.HitEffects.Add("vfx_HitSparks");
            Krampus.HitEffects.Add("sfx_greydwarf_attack_hit") ;
            Krampus.DeathEffects.Add("vfx_spawn");
            Krampus.DeathEffects.Add("sfx_trollfire_death");
            Krampus.WaterEffects.Add("vfx_water_surface");
            Krampus.CritEffects.Add("fx_crit");
            Krampus.BackStabEffects.Add("fx_backstab");
            Krampus.FootStepEffectsFrom = "Fenring";
            Krampus.AlertedEffects.Add("sfx_fenring_alerted");
            Krampus.IdleSounds.Add("sfx_fenring_idle");
            FaunaManager.Critter.Attack frost = Krampus.SetAttack("Krampus_attack_frost");
            FaunaManager.Critter.Attack iceClaw= Krampus.SetAttack("Krampus_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble = Krampus.SetAttack("Krampus_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova = Krampus.SetAttack("Krampus_attack_IceNova");
            foreach (var attack in Krampus.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }
            FaunaManager.RegisterAssetToZNetScene("Krampus_attack_flames_aoe", _Assets);
            FaunaManager.RegisterAssetToZNetScene("KrampusIceNova_aoe", _Assets);
            FaunaManager.RegisterAssetToZNetScene("Krampus_DroppedIce_AOE", _Assets);
            FaunaManager.RegisterAssetToZNetScene("fx_snowball_hit", _Assets);

            FaunaManager.ProjectileData KrampusMeteorProjectile = new FaunaManager.ProjectileData("projectile_meteor_krampus", _Assets);
            KrampusMeteorProjectile.HitEffects.Add("sfx_rock_hit");
            
            MaterialReplacer.MaterialData KrampusBeard = new MaterialReplacer.MaterialData(_Assets, "Mat_Karampus_Beard", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData KrampusBody = new MaterialReplacer.MaterialData(_Assets, "Mat_Karampus_Body", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData KrampusFur = new MaterialReplacer.MaterialData(_Assets, "Mat_Karampus_Fur", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData KrampusFurMain = new MaterialReplacer.MaterialData(_Assets, "Mat_Karampus_CoatFurMain1", MaterialReplacer.ShaderType.CustomCreature);
            
            FaunaManager.Critter Krampus2 = new("Krampus_2_RS", _Assets)
            {
                Biome = Heightmap.Biome.None,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            Krampus2.Boss = true;
            Krampus2.Name.English("Enraged Krampus");
            Krampus2.SetOnDeathTrigger("dying", 4.7f);
            Krampus2.HitEffects.Add("vfx_HitSparks");
            Krampus2.HitEffects.Add("sfx_greydwarf_attack_hit") ;
            Krampus2.DeathEffects.Add("vfx_spawn");
            Krampus2.DeathEffects.Add("sfx_trollfire_death");
            Krampus2.WaterEffects.Add("vfx_water_surface");
            Krampus2.CritEffects.Add("fx_crit");
            Krampus2.BackStabEffects.Add("fx_backstab");
            Krampus2.FootStepEffectsFrom = "Fenring";
            Krampus2.AlertedEffects.Add("sfx_fenring_alerted");
            Krampus2.IdleSounds.Add("sfx_fenring_idle");
            FaunaManager.Critter.Attack frost1 = Krampus2.SetAttack("Krampus_attack_frost");
            FaunaManager.Critter.Attack iceClaw1= Krampus2.SetAttack("Krampus_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble1 = Krampus2.SetAttack("Krampus_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova1 = Krampus2.SetAttack("Krampus_attack_IceNova");
            foreach (var attack in Krampus2.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }
            FaunaManager.Critter Loki = new("Loki_RS", _Assets)
            {
                Biome = Heightmap.Biome.None,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            Loki.Boss = true;
            Loki.Name.English("Loki");
            Loki.SetOnDeathTrigger("dying", 4.7f);
            Loki.Drops.Add("TrophyLoki", 1, 1, 1f);
            Loki.Drops.Add("LokiStaff", 1, 1, 1f);
            Loki.HitEffects.Add("vfx_HitSparks");
            Loki.HitEffects.Add("sfx_greydwarf_attack_hit") ;
            Loki.DeathEffects.Add("vfx_corpse_destruction_medium");
            Loki.DeathEffects.Add("sfx_trollfire_death");
            Loki.WaterEffects.Add("vfx_water_surface");
            Loki.CritEffects.Add("fx_crit");
            Loki.BackStabEffects.Add("fx_backstab");
            Loki.FootStepEffectsFrom = "Fenring";
            Loki.AlertedEffects.Add("sfx_fenring_alerted");
            Loki.IdleSounds.Add("sfx_fenring_idle");
            FaunaManager.Critter.Attack frost2 = Loki.SetAttack("Krampus_attack_frost");
            FaunaManager.Critter.Attack iceCla21= Loki.SetAttack("Krampus_attack_iceclaw");
            FaunaManager.Critter.Attack iceClawDouble2 = Loki.SetAttack("Krampus_attack_iceclaw_double");
            FaunaManager.Critter.Attack iceNova2 = Loki.SetAttack("Krampus_attack_IceNova");
            foreach (var attack in Loki.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_spear_hit");
            }
            var transmission1 = Krampus.Prefab.AddComponent<LevelTransmission>();
            var transmission2 = Krampus2.Prefab.AddComponent<LevelTransmission>();
            transmission1.m_spawns.Add(Krampus2.Prefab);
            transmission1.SpawnEffects.Add("fx_loki_teleport");
            transmission1.SpawnEffects.Add("sfx_dverger_heavyattack_launch");
            transmission2.m_spawns.Add(Loki.Prefab);
            transmission2.SpawnEffects.Add("fx_loki_teleport");
            transmission2.SpawnEffects.Add("sfx_dverger_heavyattack_launch");
            FaunaManager.RegisterAssetToZNetScene("fx_loki_teleport", _Assets);

            var warp = Loki.Prefab.AddComponent<Warp>();
            warp.m_startEffects.Add("fx_loki_teleport");
            warp.m_startEffects.Add("sfx_dverger_heavyattack_launch");

            FaunaManager.ProjectileData SlashProjectile = new FaunaManager.ProjectileData("Slash_Projectile", _Assets);
            SlashProjectile.HitEffects.Add("vfx_HitSparks");
            SlashProjectile.HitEffects.Add("sfx_greydwarf_attack_hit");
            FaunaManager.ProjectileData MagicProjectile = new FaunaManager.ProjectileData("Krampus_Projectile", _Assets);
            MagicProjectile.HitEffects.Add("vfx_HitSparks");
            MagicProjectile.HitEffects.Add("sfx_greydwarf_attack_hit");
        }

        private void LoadNorthTroll()
        {
            FaunaManager.Critter NorthTroll = new("NorthTroll_RS", _Assets)
            {
                Biome = Heightmap.Biome.DeepNorth,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
            };
            NorthTroll.Name.English("Jotunn");
            NorthTroll.SetOnDeathTrigger("dying", 4.7f);
            NorthTroll.Drops.Add("TrophyNorthTroll_RS", 1, 1, 0.3f);
            NorthTroll.Drops.Add("HairBundle_RS", 1, 1, 1f);
            NorthTroll.Drops.Add("BeastEye_RS", 1, 1, 0.5f);
            NorthTroll.HitEffects.Add("vfx_HitSparks");
            NorthTroll.HitEffects.Add("sfx_troll_hit");
            NorthTroll.WaterEffects.Add("vfx_water_surface");
            NorthTroll.DeathEffects.Add("vfx_corpse_destruction_large");
            NorthTroll.DeathEffects.Add("sfx_troll_death");
            NorthTroll.CritEffects.Add("fx_crit");
            NorthTroll.BackStabEffects.Add("fx_backstab");
            NorthTroll.FootStepEffectsFrom = "Troll";
            NorthTroll.AlertedEffects.Add("sfx_troll_alerted");
            NorthTroll.IdleSounds.Add("sfx_troll_idle");
            
            NorthTroll.AddEnableVisualTrigger("LOG", "SM_Troll_Club");
            FaunaManager.Critter.Attack GroundSlam = NorthTroll.SetAttack("northtroll_groundslam");
            GroundSlam.HitEffects.Add("vfx_troll_groundslam");
            GroundSlam.HitEffects.Add("sfx_troll_attack_hit");
            GroundSlam.HitTerrainEffects.Add("vfx_troll_groundslam");
            GroundSlam.HitTerrainEffects.Add("sfx_troll_rock_destroyed");
            GroundSlam.StartEffects.Add("sfx_troll_attacking");
            FaunaManager.Critter.Attack TrollSwing1 = NorthTroll.SetAttack("northtroll_log_swing_h");
            TrollSwing1.HitEffects.Add("vfx_troll_attach_hit");
            TrollSwing1.HitEffects.Add("sfx_troll_attack_hit");
            TrollSwing1.HitEffects.Add("sfx_hit_camshake");
            TrollSwing1.HitTerrainEffects.Add("vfx_troll_log_hitground");
            TrollSwing1.HitTerrainEffects.Add("sfx_troll_rock_destroyed");
            TrollSwing1.BlockEffects.Add("sfx_metal_blocked");
            TrollSwing1.BlockEffects.Add("vfx_blocked");
            TrollSwing1.BlockEffects.Add("fx_block_camshake");
            TrollSwing1.StartEffects.Add("sfx_troll_attacking");
            TrollSwing1.TriggerEffects.Add("fx_swing_camshake");
            FaunaManager.Critter.Attack TrollSwing2 = NorthTroll.SetAttack("northtroll_log_swing_v");
            TrollSwing2.HitEffects.Add("vfx_troll_attach_hit");
            TrollSwing2.HitEffects.Add("sfx_troll_attack_hit");
            TrollSwing2.HitEffects.Add("sfx_hit_camshake");
            TrollSwing2.HitTerrainEffects.Add("vfx_troll_log_hitground");
            TrollSwing2.HitTerrainEffects.Add("sfx_troll_rock_destroyed");
            TrollSwing2.BlockEffects.Add("sfx_metal_blocked");
            TrollSwing2.BlockEffects.Add("vfx_blocked");
            TrollSwing2.BlockEffects.Add("fx_block_camshake");
            TrollSwing2.StartEffects.Add("sfx_troll_attacking");
            TrollSwing2.TriggerEffects.Add("fx_swing_camshake");
            FaunaManager.Critter.Attack Punch = NorthTroll.SetAttack("northtroll_punch");
            Punch.HitEffects.Add("vfx_troll_attach_hit");
            Punch.HitEffects.Add("sfx_troll_attack_hit");
            Punch.HitEffects.Add("vfx_troll_rock_destroyed");
            Punch.HitEffects.Add("sfx_troll_rock_destroyed");
            Punch.StartEffects.Add("sfx_troll_attacking");
            Punch.TriggerEffects.Add("fx_swing_camshake");
            FaunaManager.Critter.Attack Throw = NorthTroll.SetAttack("northtroll_throw");
            Throw.HitEffects.Add("vfx_HitSparks");
            Throw.HitEffects.Add("sfx_greydwarf_attack_hit");
            Throw.StartEffects.Add("sfx_imp_fireball_launch");

            FaunaManager.ProjectileData TrollProjectile = new("trollnorth_throw_projectile", _Assets);
            TrollProjectile.HitEffects.Add("sfx_troll_rock_destroyed");
            TrollProjectile.HitEffects.Add("vfx_troll_rock_destroyed");
            TrollProjectile.HitEffects.Add("sfx_troll_attack_hit");
            
            FaunaManager.RegisterAssetToZNetScene("northtroll_groundslam_aoe", _Assets);
            MaterialReplacer.MaterialData TrollFur = new MaterialReplacer.MaterialData(_Assets, "Mat_Troll_Fur_Skin1", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData TrollBody = new MaterialReplacer.MaterialData(_Assets, "Mat_Troll_Body_Skin1", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData TrollClub = new MaterialReplacer.MaterialData(_Assets, "Mat_Troll_Club_Skin1", MaterialReplacer.ShaderType.CustomCreature);
        }
        
        private void LoadWereWolf()
        {
            FaunaManager.Critter WereWolf = new("WereWolf_RS", _Assets)
            {
                Biome = Heightmap.Biome.DeepNorth,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
                TimeOfDay = FaunaManager.TimeOfDay.Night
            };
            WereWolf.Name.English("Were-Wolf");
            WereWolf.Drops.Add("HairBundle_RS", 1, 1, 1f);
            WereWolf.Drops.Add("TrophyWereWolf_RS", 1, 1, 0.1f);
            WereWolf.Drops.Add("ArmorWereWolfChest_RS", 1, 1, 0.05f);
            WereWolf.Drops.Add("ArmorWereWolfLegs_RS", 1, 1, 0.05f);
            WereWolf.Drops.Add("FistWereWolfClaw_RS", 1, 1, 0.05f);
            WereWolf.SetOnDeathTrigger("dying", 4.7f);
            WereWolf.HitEffects.Add("vfx_HitSparks");
            WereWolf.HitEffects.Add("sfx_fenring_hurt");
            WereWolf.CritEffects.Add("fx_crit");
            WereWolf.BackStabEffects.Add("fx_backstab");
            WereWolf.DeathEffects.Add("vfx_corpse_destruction_medium");
            WereWolf.DeathEffects.Add("sfx_fenring_death");
            WereWolf.WaterEffects.Add("vfx_water_surface");
            WereWolf.FootStepEffectsFrom = "Fenring";
            WereWolf.AlertedEffects.Add("sfx_fenring_alerted");
            WereWolf.IdleSounds.Add("sfx_fenring_idle");
            var backHand = WereWolf.SetAttack("WereWolf_attack_backhand");
            var bite = WereWolf.SetAttack("WereWolf_attack_bite");
            var claw = WereWolf.SetAttack("WereWolf_attack_claw");
            var attackDouble = WereWolf.SetAttack("WereWolf_attack_double");
            var down = WereWolf.SetAttack("WereWolf_attack_down");
            foreach (var attack in WereWolf.Attacks)
            {
                attack.HitEffects.Add("vfx_HitSparks");
                attack.HitEffects.Add("sfx_fenring_claw_hit");
                attack.StartEffects.Add("sfx_fenring_claw_start");
                attack.TrailStartEffects.Add("sfx_fenring_claw_trailstart");
            }
            var jump = WereWolf.SetAttack("WereWolf_attack_jump");
            jump.HitEffects.Add("vfx_HitSparks");
            jump.HitEffects.Add("sfx_fenring_claw_hit");
            jump.StartEffects.Add("sfx_fenring_jump_start");
            jump.TriggerEffects.Add("sfx_fenring_jump_trigger");
            jump.TrailStartEffects.Add("sfx_fenring_jump_trailstart");
            MaterialReplacer.MaterialData WereWolfAccessories = new MaterialReplacer.MaterialData(_Assets, "Mat_Werewolf_Acessories", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WereWolfArmor = new MaterialReplacer.MaterialData(_Assets, "Mat_Werewolf_Armor", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WereWolfBody = new MaterialReplacer.MaterialData(_Assets, "Mat_Werewolf_Body_black", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WereWolfHands = new MaterialReplacer.MaterialData(_Assets, "Mat_Werewolf_Hands", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WereWolfLegs = new MaterialReplacer.MaterialData(_Assets, "Mat_Werewolf_Leegs", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WereWolfShoulders = new MaterialReplacer.MaterialData(_Assets, "Mat_Werewolf_Shoulders", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData WereWolfFurBlack = new MaterialReplacer.MaterialData(_Assets, "Mat_Werewolf_fur_black", MaterialReplacer.ShaderType.CustomCreature);
            MaterialReplacer.MaterialData Stone = new MaterialReplacer.MaterialData(_Assets, "stone1", MaterialReplacer.ShaderType.RockShader);
        }

        private void LoadLizardFish()
        {
            FaunaManager.Critter LizardFish = new("LizardFish_RS", _Assets)
            {
                Biome = Heightmap.Biome.DeepNorth,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
                TimeOfDay = FaunaManager.TimeOfDay.Day
            };
            LizardFish.Name.English("Skaldrik");
            LizardFish.SetOnDeathTrigger("dying", 4.7f);
            LizardFish.Drops.Add("TrophyLizardFish_RS", 1, 1, 0.1f);
            LizardFish.Drops.Add("LizardMeatRaw_RS", 1, 1, 1f);
            LizardFish.Drops.Add("LizardTail_RS", 1, 1, 1f);
            LizardFish.HitEffects.Add("vfx_HitSparks");
            LizardFish.HitEffects.Add("sfx_greydwarf_attack_hit") ;
            LizardFish.DeathEffects.Add("vfx_corpse_destruction_small");
            LizardFish.DeathEffects.Add("sfx_greydwarf_death");
            LizardFish.WaterEffects.Add("vfx_water_surface");
            LizardFish.CritEffects.Add("fx_crit");
            LizardFish.BackStabEffects.Add("fx_backstab");
            LizardFish.FootStepEffectsFrom = "Neck";

            FaunaManager.ProjectileData LizardFishProjectile = new FaunaManager.ProjectileData("LizardFish_throw_projectile", _Assets);
            LizardFishProjectile.HitEffects.Add("sfx_troll_rock_destroyed");
            LizardFishProjectile.HitEffects.Add("vfx_troll_rock_destroyed");
            LizardFishProjectile.HitEffects.Add("sfx_troll_attack_hit");
        }

        private void LoadCatFish()
        {
            FaunaManager.Critter CatFish = new("CatFish_RS", _Assets)
            {
                Biome = Heightmap.Biome.DeepNorth,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
                TimeOfDay = FaunaManager.TimeOfDay.Day
            };
            CatFish.Name.English("Sildrak");
            CatFish.SetOnDeathTrigger("dying", 4.7f);
            CatFish.Drops.Add("TrophyCatFish_RS", 1, 1, 0.1f);
            CatFish.Drops.Add("Fish12", 1, 2, 1f);
            CatFish.Drops.Add("Fish10", 1, 2, 1f);
            CatFish.Drops.Add("SpearCatFish_RS", 1, 1, 0.05f);
            CatFish.HitEffects.Add("vfx_HitSparks");
            CatFish.HitEffects.Add("sfx_greydwarf_attack_hit") ;
            CatFish.DeathEffects.Add("vfx_corpse_destruction_small");
            CatFish.DeathEffects.Add("sfx_greydwarf_death");
            CatFish.WaterEffects.Add("vfx_water_surface");
            CatFish.CritEffects.Add("fx_crit");
            CatFish.BackStabEffects.Add("fx_backstab");
            CatFish.FootStepEffectsFrom = "Neck";
            FaunaManager.Critter.Attack Throw = CatFish.SetAttack("CatFish_throw");
            Throw.HitEffects.Add("vfx_HitSparks");
            Throw.HitEffects.Add("sfx_spear_hit");
            FaunaManager.Critter.Attack CatFishAttack = CatFish.SetAttack("CatFish_attack");
            CatFishAttack.HitEffects.Add("vfx_HitSparks");
            CatFishAttack.HitEffects.Add("sfx_spear_hit");
        }

        private void LoadCreeper()
        {
            FaunaManager.Critter Creeper = new("Creeper_RS", _Assets)
            {
                Biome = Heightmap.Biome.DeepNorth,
                SpawnDistance = 20f,
                SpawnRadiusMin = 20f,
                SpawnRadiusMax = 100f,
                TimeOfDay = FaunaManager.TimeOfDay.Night
            };
            Creeper.Name.English("Myrvarg");
            Creeper.SetOnDeathTrigger("dying", 4.7f);
            Creeper.Drops.Add("TrophyCreeper_RS", 1, 1, 0.3f);
            Creeper.Drops.Add("UncookedBeastLeg_RS", 1, 1, 0.5f);
            Creeper.HitEffects.Add("vfx_HitSparks");
            Creeper.HitEffects.Add("sfx_greydwarf_attack_hit") ;
            Creeper.DeathEffects.Add("vfx_corpse_destruction_small");
            Creeper.DeathEffects.Add("sfx_greydwarf_death");
            Creeper.CritEffects.Add("fx_crit");
            Creeper.BackStabEffects.Add("fx_backstab");
            Creeper.FootStepEffectsFrom = "Ulv";
            Creeper.WaterEffects.Add("vfx_water_surface");
            FaunaManager.Critter.Attack CreeperThrow = Creeper.SetAttack("Creeper_throw");
            CreeperThrow.TriggerEffects.Add("sfx_askvin_alert");
        }

        private void LoadButterfly()
        {
            FaunaManager.Bird MysticalButterfly = new FaunaManager.Bird("Butterfly_RS", _Assets);
            MysticalButterfly.DestroyedEffects.Add("vfx_greydwarf_death");
            MysticalButterfly.DestroyedEffects.Add("sfx_crow_death");
            MysticalButterfly.Biome = Heightmap.Biome.DeepNorth;
            MysticalButterfly.SpawnTimeOfDay = FaunaManager.TimeOfDay.Night;
            MysticalButterfly.Health = 1;
            MysticalButterfly.Range = 10f;
            MysticalButterfly.Speed = 1f;
            MysticalButterfly.MaxSpawned = 1;
            MysticalButterfly.SpawnInterval = 1000;
            MysticalButterfly.Drops.Add("RS_ForsakenEssence", 1, 1, 1f);

            MaterialReplacer.MaterialData ButterflyBody = new MaterialReplacer.MaterialData(_Assets, "butterflyMAT", MaterialReplacer.ShaderType.CustomCreature);
        }

        private void LoadFairy()
        {
            FaunaManager.Bird Fairy = new FaunaManager.Bird("Fairy_RS", _Assets);
            Fairy.Biome = Heightmap.Biome.DeepNorth;
            Fairy.Health = 1;
            Fairy.Range = 3f;
            Fairy.Speed = 0.1f;
            Fairy.MaxSpawned = 1;
            Fairy.SpawnInterval = 1000;
            Fairy.Drops.Add("Navi_RS", 1, 1, 0.1f);
        }
        
        private void LoadSquid()
        {
            FaunaManager.Critter Squid = new FaunaManager.Critter("Squid_RS", _Assets);
            Squid.Name.English("Akkar");
            Squid.Drops.Add("Tar", 1, 5, 1f);
            Squid.Drops.Add("SquidMeat_RS", 1, 1, 1f);
            Squid.Drops.Add("TrophySquid_RS", 1, 1, 0.1f);
            Squid.Health = 2000f;
            Squid.Biome = Heightmap.Biome.DeepNorth;
            Squid.BiomeArea = Heightmap.BiomeArea.Edge;
            Squid.MaxAltitude = 10f;
            Squid.HitEffects.Add("vfx_serpent_hut");
            Squid.HitEffects.Add("sfx_leech_hit") ;
            Squid.CritEffects.Add("fx_crit");
            Squid.BackStabEffects.Add("fx_backstab");
            Squid.DeathEffects.Add("vfx_corpse_destruction_small");
            Squid.DeathEffects.Add("sfx_greydwarf_death");
            Squid.WaterEffects.Add("vfx_serpent_watersurface");
            FaunaManager.Critter.Attack spit = Squid.SetAttack("Squid_spit");
            spit.GetProjectileFrom = "blobtar_projectile_tarball";
            // FaunaManager.ProjectileData SquidProjectile = new FaunaManager.ProjectileData("Squid_projectile", _Assets);
            // SquidProjectile.HitEffects.Add("fx_BonemawSerpent_Spit_Hit");
            // SquidProjectile.HitEffects.Add("sfx_bonemaw_serpent_spit_hit");
        }

        private void Update()
        {
            EnableVisual.UpdateAttackVisual();
        }

        private void OnDestroy() => Config.Save();
        
        public void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                BestiaryLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                BestiaryLogger.LogError($"There was an issue loading your {ConfigFileName}");
                BestiaryLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
        
        public ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        public ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }
    }
}