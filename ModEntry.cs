using System;
using System.Linq;
using System.Reflection;
using GenericModConfigMenu;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Buffs;

namespace SkillProgression;

public sealed class ModEntry : Mod
{
    private const string ModDataKey = "YolBukik.SkillProgression";
    private const string BaseHealthKey = ModDataKey + "/BaseMaxHealth";
    private const string CombatLevelKey = ModDataKey + "/AppliedCombatLevel";
    private const string DefenseBuffId = ModDataKey + ".Defense";

    internal static ModConfig Config { get; private set; } = new();

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();

        I18n.Init(helper.Translation);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Player.LevelChanged += OnLevelChanged;

        Harmony harmony = new(ModManifest.UniqueID);

        FarmingQualityPatch.Initialize(Monitor, harmony);
        ArtisanSpeedPatch.Initialize(Monitor, harmony);
    }

    private void OnGameLaunched(
        object? sender,
        GameLaunchedEventArgs e
    )
    {
        var gmcm =
            Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                "spacechase0.GenericModConfigMenu"
            );

        if (gmcm == null)
        {
            Monitor.Log(
                "Generic Mod Config Menu was not found.",
                LogLevel.Warn
            );

            return;
        }

        // =========================================================
        // MAIN PAGE
        // =========================================================

        gmcm.Register(
            ModManifest,

            reset: () =>
            {
                Config = new ModConfig();
            },

            save: () =>
            {
                Helper.WriteConfig(Config);

                if (Context.IsWorldReady)
                    ApplyAllProgressions();
            }
        );

        // ---------------------------------------------------------
        // PAGE LINKS
        // ---------------------------------------------------------

        gmcm.AddPageLink(
            ModManifest,
            "combat",
            () => "⚔️ Combat",
            () => "Configure Combat progression."
        );

        gmcm.AddPageLink(
            ModManifest,
            "farming",
            () => "🌱 Farming",
            () => "Configure Farming progression."
        );

        // =========================================================
        // COMBAT PAGE
        // =========================================================

        gmcm.AddPage(
            ModManifest,
            "combat",
            () => "⚔️ Combat"
        );

        // ---------------------------------------------------------
        // MAX HEALTH
        // ---------------------------------------------------------

        gmcm.AddSubHeader(
            ModManifest,
            () => "❤️ Max Health per Level"
        );

        gmcm.AddParagraph(
            ModManifest,
            () => I18n.Get(
                "config.max_health_description"
            )
        );

        for (int i = 0; i < 10; i++)
        {
            int level = i + 1;

            gmcm.AddNumberOption(
                ModManifest,

                name: () => I18n.Get(
                    "config.level",
                    new { level }
                ),

                tooltip: () => I18n.Get(
                    "config.max_health_tooltip",
                    new { level }
                ),

                getValue: () =>
                    Config.MaxHealthBonuses[level - 1],

                setValue: value =>
                    Config.MaxHealthBonuses[level - 1] = value,

                min: 0,
                max: 300,
                interval: 1,

                fieldId:
                    $"combat.max_health.level{level}"
            );
        }

        // ---------------------------------------------------------
        // DEFENSE
        // ---------------------------------------------------------

        gmcm.AddSubHeader(
            ModManifest,
            () => "🛡️ Defense per Level"
        );

        gmcm.AddParagraph(
            ModManifest,
            () => I18n.Get(
                "config.defense_description"
            )
        );

        for (int i = 0; i < 10; i++)
        {
            int level = i + 1;

            gmcm.AddNumberOption(
                ModManifest,

                name: () => I18n.Get(
                    "config.level",
                    new { level }
                ),

                tooltip: () => I18n.Get(
                    "config.defense_tooltip",
                    new { level }
                ),

                getValue: () =>
                    Config.DefenseBonuses[level - 1],

                setValue: value =>
                    Config.DefenseBonuses[level - 1] = value,

                min: 0,
                max: 100,
                interval: 1,

                fieldId:
                    $"combat.defense.level{level}"
            );
        }

        // =========================================================
        // FARMING PAGE
        // =========================================================

        gmcm.AddPage(
            ModManifest,
            "farming",
            () => "🌱 Farming"
        );

        // ---------------------------------------------------------
        // HARVEST QUALITY
        // ---------------------------------------------------------

        gmcm.AddSubHeader(
            ModManifest,
            () => I18n.Get(
                "config.farming_quality_section"
            )
        );

        gmcm.AddParagraph(
            ModManifest,
            () => I18n.Get(
                "config.farming_quality_description"
            )
        );

        gmcm.AddBoolOption(
            ModManifest,

            name: () => I18n.Get(
                "config.enable_quality_boost"
            ),

            getValue: () =>
                Config.EnableFarmingQualityBoost,

            setValue: value =>
                Config.EnableFarmingQualityBoost = value,

            fieldId:
                "farming.quality.enabled"
        );

        for (int level = 1; level <= 10; level++)
        {
            int currentLevel = level;

            gmcm.AddSubHeader(
                ModManifest,
                () => I18n.Get(
                    "config.farming_level",
                    new
                    {
                        level = currentLevel
                    }
                )
            );

            gmcm.AddNumberOption(
                ModManifest,

                name: () => I18n.Get(
                    "config.normal_to_silver_chance"
                ),

                tooltip: () => I18n.Get(
                    "config.farming_quality_chance_tooltip",
                    new
                    {
                        level = currentLevel
                    }
                ),

                getValue: () =>
                    Config.FarmingQualityNormalToSilver[
                        currentLevel - 1
                    ],

                setValue: value =>
                    Config.FarmingQualityNormalToSilver[
                        currentLevel - 1
                    ] = value,

                min: 0,
                max: 100,
                interval: 1,

                fieldId:
                    $"farming.quality.normal_to_silver.level{currentLevel}"
            );

            gmcm.AddNumberOption(
                ModManifest,

                name: () => I18n.Get(
                    "config.silver_to_gold_chance"
                ),

                tooltip: () => I18n.Get(
                    "config.farming_quality_chance_tooltip",
                    new
                    {
                        level = currentLevel
                    }
                ),

                getValue: () =>
                    Config.FarmingQualitySilverToGold[
                        currentLevel - 1
                    ],

                setValue: value =>
                    Config.FarmingQualitySilverToGold[
                        currentLevel - 1
                    ] = value,

                min: 0,
                max: 100,
                interval: 1,

                fieldId:
                    $"farming.quality.silver_to_gold.level{currentLevel}"
            );

            gmcm.AddNumberOption(
                ModManifest,

                name: () => I18n.Get(
                    "config.gold_to_iridium_chance"
                ),

                tooltip: () => I18n.Get(
                    "config.farming_quality_chance_tooltip",
                    new
                    {
                        level = currentLevel
                    }
                ),

                getValue: () =>
                    Config.FarmingQualityGoldToIridium[
                        currentLevel - 1
                    ],

                setValue: value =>
                    Config.FarmingQualityGoldToIridium[
                        currentLevel - 1
                    ] = value,

                min: 0,
                max: 100,
                interval: 1,

                fieldId:
                    $"farming.quality.gold_to_iridium.level{currentLevel}"
            );
        }

        // ---------------------------------------------------------
        // HARVEST QUANTITY
        // ---------------------------------------------------------

        gmcm.AddSubHeader(
            ModManifest,
            () => I18n.Get(
                "config.harvest_quantity_section"
            )
        );

        gmcm.AddParagraph(
            ModManifest,
            () => I18n.Get(
                "config.harvest_quantity_description"
            )
        );

        gmcm.AddBoolOption(
            ModManifest,

            name: () => I18n.Get(
                "config.enable_quantity_boost"
            ),

            getValue: () =>
                Config.EnableHarvestQuantityBoost,

            setValue: value =>
                Config.EnableHarvestQuantityBoost = value,

            fieldId:
                "farming.quantity.enabled"
        );

        for (int level = 1; level <= 10; level++)
        {
            int currentLevel = level;

            gmcm.AddNumberOption(
                ModManifest,

                name: () => I18n.Get(
                    "config.quantity_chance",
                    new
                    {
                        level = currentLevel
                    }
                ),

                tooltip: () => I18n.Get(
                    "config.quantity_chance_tooltip",
                    new
                    {
                        level = currentLevel
                    }
                ),

                getValue: () =>
                    Config.HarvestQuantityChance[
                        currentLevel - 1
                    ],

                setValue: value =>
                    Config.HarvestQuantityChance[
                        currentLevel - 1
                    ] = value,

                min: 0,
                max: 100,
                interval: 1,

                fieldId:
                    $"farming.quantity.level{currentLevel}"
            );
        }

        // ---------------------------------------------------------
        // ARTISAN SPEED
        // ---------------------------------------------------------

        gmcm.AddSubHeader(
            ModManifest,
            () => I18n.Get(
                "config.artisan_speed_section"
            )
        );

        gmcm.AddParagraph(
            ModManifest,
            () => I18n.Get(
                "config.artisan_speed_description"
            )
        );

        gmcm.AddBoolOption(
            ModManifest,

            name: () => I18n.Get(
                "config.enable_artisan_speed"
            ),

            getValue: () =>
                Config.EnableArtisanSpeed,

            setValue: value =>
                Config.EnableArtisanSpeed = value,

            fieldId:
                "farming.machine_speed.enabled"
        );

        for (int i = 0; i < 10; i++)
        {
            int level = i + 1;

            gmcm.AddNumberOption(
                ModManifest,

                name: () => I18n.Get(
                    "config.artisan_speed_reduction",
                    new
                    {
                        level = level
                    }
                ),

                tooltip: () => I18n.Get(
                    "config.artisan_speed_tooltip",
                    new
                    {
                        level = level
                    }
                ),

                getValue: () =>
                    Config.ArtisanSpeedReduction[
                        level - 1
                    ],

                setValue: value =>
                    Config.ArtisanSpeedReduction[
                        level - 1
                    ] = value,

                min: 0,
                max: 100,
                interval: 1,

                fieldId:
                    $"farming.machine_speed.level{level}"
            );
        }

        // =========================================================
        // RETURN TO MAIN PAGE
        // =========================================================

        gmcm.AddPage(
            ModManifest,
            ""
        );
    }

    private void OnSaveLoaded(
        object? sender,
        SaveLoadedEventArgs e
    )
    {
        ApplyAllProgressions();
    }

    private void OnDayStarted(
        object? sender,
        DayStartedEventArgs e
    )
    {
        ApplyAllProgressions();
    }

    private void OnLevelChanged(
        object? sender,
        LevelChangedEventArgs e
    )
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Skill == SkillType.Combat)
            ApplyAllProgressions();
    }

    private void ApplyAllProgressions()
    {
        if (!Context.IsWorldReady)
            return;

        Farmer player = Game1.player;

        int combatLevel =
            player.GetUnmodifiedSkillLevel(4);

        int vanillaCombatHealth =
            GetVanillaCombatHealthBonus(
                player,
                combatLevel
            );

        int baseMaxHealth;

        if (
            player.modData.TryGetValue(
                BaseHealthKey,
                out string? storedBase
            )
            &&
            int.TryParse(
                storedBase,
                out int parsedBase
            )
        )
        {
            baseMaxHealth = parsedBase;
        }
        else
        {
            baseMaxHealth =
                Math.Max(
                    1,
                    player.maxHealth -
                    vanillaCombatHealth
                );

            player.modData[BaseHealthKey] =
                baseMaxHealth.ToString();
        }

        player.maxHealth =
            baseMaxHealth +
            vanillaCombatHealth +
            GetIntBonus(
                Config.MaxHealthBonuses,
                combatLevel
            );

        ApplyDefenseBuff(
            player,
            combatLevel
        );

        player.modData[CombatLevelKey] =
            combatLevel.ToString();
    }

    private int GetIntBonus(
        int[] bonuses,
        int level
    )
    {
        return
            level >= 1 &&
            level <= bonuses.Length
                ? bonuses[level - 1]
                : 0;
    }

    private int GetVanillaCombatHealthBonus(
        Farmer player,
        int combatLevel
    )
    {
        if (combatLevel <= 0)
            return 0;

        int bonus =
            combatLevel * 5;

        if (
            player.professions.Contains(
                Farmer.fighter
            )
        )
        {
            bonus += 15;
        }

        if (
            player.professions.Contains(
                Farmer.defender
            )
        )
        {
            bonus += 25;
        }

        return bonus;
    }

    private void ApplyDefenseBuff(
        Farmer player,
        int combatLevel
    )
    {
        if (
            player.buffs.AppliedBuffs.ContainsKey(
                DefenseBuffId
            )
        )
        {
            player.buffs.Remove(
                DefenseBuffId
            );
        }

        int defenseBonus =
            GetIntBonus(
                Config.DefenseBonuses,
                combatLevel
            );

        if (defenseBonus <= 0)
            return;

        BuffAttributesData attributes =
            new BuffAttributesData
            {
                Defense = defenseBonus
            };

        BuffEffects effects =
            new BuffEffects(attributes);

        ConstructorInfo? constructor =
            typeof(Buff)
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance
                )
                .FirstOrDefault(
                    c =>
                        c.GetParameters().Length == 10
                );

        if (constructor == null)
            return;

        object?[] args =
        {
            DefenseBuffId,
            ModManifest.Name,
            "Defense",
            0,
            null,
            0,
            effects,
            null,
            null,
            null
        };

        player.buffs.Apply(
            (Buff)constructor.Invoke(args)
        );
    }
}
