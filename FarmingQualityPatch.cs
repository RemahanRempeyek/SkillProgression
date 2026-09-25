using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace SkillProgression;

internal static class FarmingQualityPatch
{
    private static IMonitor Monitor = null!;

    public static void Initialize(IMonitor monitor, Harmony harmony)
    {
        Monitor = monitor;

        MethodInfo? target = AccessTools.Method(
            typeof(Crop),
            nameof(Crop.harvest)
        );

        if (target == null)
        {
            Monitor.Log(
                "Could not find Crop.harvest for Farming patches.",
                LogLevel.Error
            );

            return;
        }

        harmony.Patch(
            target,
            transpiler: new HarmonyMethod(
                typeof(FarmingQualityPatch),
                nameof(Transpiler)
            )
        );

        Monitor.Log(
            "Farming Quality and Harvest Quantity patches applied.",
            LogLevel.Info
        );
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);

        MethodInfo? modifyQualityMethod = AccessTools.Method(
            typeof(FarmingQualityPatch),
            nameof(ModifyQuality)
        );

        MethodInfo? modifyQuantityMethod = AccessTools.Method(
            typeof(FarmingQualityPatch),
            nameof(ModifyQuantity)
        );

        if (modifyQualityMethod == null ||
            modifyQuantityMethod == null)
        {
            Monitor.Log(
                "Could not find Farming modification methods.",
                LogLevel.Error
            );

            return codes;
        }

        bool qualityInjected = false;
        bool quantityInjected = false;

        /*
         * ------------------------------------------------------------
         * FARMING QUALITY
         * ------------------------------------------------------------
         *
         * Stardew Valley 1.6.15:
         * Vanilla crop quality is stored in local 14
         * after the vanilla quality calculation and clamp.
         */

        for (int i = 0; i < codes.Count; i++)
        {
            if (qualityInjected)
                break;

            if (!IsStoreLocal(codes[i]))
                continue;

            int? localIndex = GetLocalIndex(codes[i]);

            if (localIndex != 14)
                continue;

            bool looksLikeQualityStore = false;

            int start = Math.Max(0, i - 8);

            for (int j = start; j < i; j++)
            {
                if (codes[j].opcode == OpCodes.Call ||
                    codes[j].opcode == OpCodes.Callvirt)
                {
                    looksLikeQualityStore = true;
                    break;
                }
            }

            if (!looksLikeQualityStore)
                continue;

            object local = codes[i].operand!;

            codes.InsertRange(
                i + 1,
                new[]
                {
                    new CodeInstruction(OpCodes.Ldloc, local),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, modifyQualityMethod),
                    new CodeInstruction(OpCodes.Stloc, local)
                }
            );

            qualityInjected = true;

            Monitor.Log(
                "Injected Farming quality bonus into Crop.harvest local 14.",
                LogLevel.Trace
            );
        }

        /*
         * ------------------------------------------------------------
         * HARVEST QUANTITY
         * ------------------------------------------------------------
         *
         * Stardew Valley 1.6.15:
         *
         * local 15 = final harvest quantity.
         *
         * After vanilla quantity calculation, the game enters:
         *
         *   ldc.i4.0
         *   stloc.s 25
         *
         * and then loops until local 25 reaches local 15.
         *
         * We inject immediately before that counter initialization.
         */

        for (int i = 0; i < codes.Count - 1; i++)
        {
            if (quantityInjected)
                break;

            if (codes[i].opcode != OpCodes.Ldc_I4_0)
                continue;

            if (!IsStoreLocal(codes[i + 1]))
                continue;

            int? counterLocal = GetLocalIndex(codes[i + 1]);

            if (counterLocal != 25)
                continue;

            /*
             * Confirm this is followed by the harvest-item loop.
             */
            bool looksLikeHarvestLoop = false;

            int end = Math.Min(codes.Count, i + 20);

            for (int j = i + 2; j < end; j++)
            {
                if (codes[j].opcode == OpCodes.Ldloc ||
                    codes[j].opcode == OpCodes.Ldloc_S)
                {
                    int? local = GetLocalIndex(codes[j]);

                    if (local == 16)
                    {
                        looksLikeHarvestLoop = true;
                        break;
                    }
                }
            }

            if (!looksLikeHarvestLoop)
                continue;

            /*
             * Find the existing local 15 value.
             */
            CodeInstruction loadQuantity =
                new(OpCodes.Ldloc, 15);

            CodeInstruction loadCrop =
                new(OpCodes.Ldarg_0);

            CodeInstruction callModify =
                new(OpCodes.Call, modifyQuantityMethod);

            CodeInstruction storeQuantity =
                new(OpCodes.Stloc, 15);

            codes.InsertRange(
                i,
                new[]
                {
                    loadQuantity,
                    loadCrop,
                    callModify,
                    storeQuantity
                }
            );

            quantityInjected = true;

            Monitor.Log(
                "Injected Harvest Quantity bonus before the vanilla harvest loop.",
                LogLevel.Trace
            );
        }

        if (!qualityInjected)
        {
            Monitor.Log(
                "Could not locate vanilla crop quality local in Crop.harvest. " +
                "Farming Quality patch was not injected.",
                LogLevel.Warn
            );
        }

        if (!quantityInjected)
        {
            Monitor.Log(
                "Could not locate vanilla harvest quantity loop in Crop.harvest. " +
                "Harvest Quantity patch was not injected.",
                LogLevel.Warn
            );
        }

        return codes;
    }

    private static bool IsStoreLocal(CodeInstruction instruction)
    {
        return instruction.opcode == OpCodes.Stloc
            || instruction.opcode == OpCodes.Stloc_S
            || instruction.opcode == OpCodes.Stloc_0
            || instruction.opcode == OpCodes.Stloc_1
            || instruction.opcode == OpCodes.Stloc_2
            || instruction.opcode == OpCodes.Stloc_3;
    }

    private static int? GetLocalIndex(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Stloc_0)
            return 0;

        if (instruction.opcode == OpCodes.Stloc_1)
            return 1;

        if (instruction.opcode == OpCodes.Stloc_2)
            return 2;

        if (instruction.opcode == OpCodes.Stloc_3)
            return 3;

        if (instruction.operand is LocalBuilder local)
            return local.LocalIndex;

        return null;
    }

    private static int? GetLocalIndexFromLoad(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldloc_0)
            return 0;

        if (instruction.opcode == OpCodes.Ldloc_1)
            return 1;

        if (instruction.opcode == OpCodes.Ldloc_2)
            return 2;

        if (instruction.opcode == OpCodes.Ldloc_3)
            return 3;

        if (instruction.operand is LocalBuilder local)
            return local.LocalIndex;

        return null;
    }

    private static int ModifyQuality(int quality, Crop crop)
    {
        if (!ModEntry.Config.EnableFarmingQualityBoost)
            return quality;

        Farmer player = Game1.player;

        // Use base Farming level so food buffs don't access
        // a nonexistent level 11+ configuration entry.
        int farmingLevel = player.farmingLevel.Value;

        if (farmingLevel < 1)
            return quality;

        int configLevel = Math.Clamp(
            farmingLevel,
            1,
            ModEntry.Config.FarmingQualityNormalToSilver.Length
        );

        int index = configLevel - 1;

        double normalToSilver =
            Math.Clamp(
                ModEntry.Config.FarmingQualityNormalToSilver[index],
                0,
                100
            ) / 100.0;

        double silverToGold =
            Math.Clamp(
                ModEntry.Config.FarmingQualitySilverToGold[index],
                0,
                100
            ) / 100.0;

        double goldToIridium =
            Math.Clamp(
                ModEntry.Config.FarmingQualityGoldToIridium[index],
                0,
                100
            ) / 100.0;

        Random random = Game1.random;

        /*
         * Normal -> Silver
         */
        if (quality == 0 &&
            random.NextDouble() < normalToSilver)
        {
            quality = 1;
        }

        /*
         * Silver -> Gold
         */
        if (quality == 1 &&
            random.NextDouble() < silverToGold)
        {
            quality = 2;
        }

        /*
         * Gold -> Iridium
         */
        if (quality == 2 &&
            random.NextDouble() < goldToIridium)
        {
            quality = 4;
        }

        /*
         * Respect the crop's own maximum quality.
         */
        int maxQuality = 4;

        var data = crop.GetData();

        if (data != null &&
            data.HarvestMaxQuality.HasValue)
        {
            maxQuality = data.HarvestMaxQuality.Value;
        }

        return Math.Min(quality, maxQuality);
    }

    private static int ModifyQuantity(int quantity, Crop crop)
    {
        if (!ModEntry.Config.EnableHarvestQuantityBoost)
            return quantity;

        Farmer player = Game1.player;

        // Use base Farming level.
        int farmingLevel = player.farmingLevel.Value;

        if (farmingLevel < 1)
            return quantity;

        int configLevel = Math.Clamp(
            farmingLevel,
            1,
            ModEntry.Config.HarvestQuantityChance.Length
        );

        int index = configLevel - 1;

        double chance =
            Math.Clamp(
                ModEntry.Config.HarvestQuantityChance[index],
                0,
                100
            ) / 100.0;

        Random random = Game1.random;

        /*
         * One independent +1 harvest roll.
         *
         * Farming 5  = 10%
         * Farming 6  = 15%
         * Farming 7  = 20%
         * Farming 8  = 25%
         * Farming 9  = 30%
         * Farming 10 = 40%
         */
        if (random.NextDouble() < chance)
        {
            quantity++;
        }

        return quantity;
    }
}
