using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;

namespace SkillProgression;

internal static class ArtisanSpeedPatch
{
    private static IMonitor? Monitor;

    public static void Initialize(
        IMonitor monitor,
        Harmony harmony
    )
    {
        Monitor = monitor;

        MethodInfo? method =
            AccessTools.Method(
                typeof(StardewValley.Object),
                "OutputMachine"
            );

        if (method == null)
        {
            Monitor.Log(
                "OutputMachine method not found.",
                LogLevel.Error
            );
            return;
        }

        harmony.Patch(
            method,
            transpiler: new HarmonyMethod(
                typeof(ArtisanSpeedPatch),
                nameof(Transpiler)
            )
        );

        Monitor.Log(
            "Artisan/Machine speed patch applied.",
            LogLevel.Info
        );
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        List<CodeInstruction> codes =
            new(instructions);

        MethodInfo? setter =
            AccessTools.PropertySetter(
                typeof(StardewValley.Object),
                "MinutesUntilReady"
            );

        MethodInfo? modifyMethod =
            AccessTools.Method(
                typeof(ArtisanSpeedPatch),
                nameof(ModifyTime)
            );

        if (setter == null || modifyMethod == null)
        {
            Monitor?.Log(
                "Could not find MinutesUntilReady setter or ModifyTime.",
                LogLevel.Error
            );
            return codes;
        }

        for (int i = 0; i < codes.Count; i++)
        {
            if (
                codes[i].opcode == OpCodes.Call &&
                codes[i].operand is MethodInfo called &&
                called == setter
            )
            {
                codes.InsertRange(
                    i,
                    new[]
                    {
                        new CodeInstruction(
                            OpCodes.Ldarg_1
                        ),

                        new CodeInstruction(
                            OpCodes.Ldarg_S,
                            (byte)4
                        ),

                        new CodeInstruction(
                            OpCodes.Ldarg_0
                        ),

                        new CodeInstruction(
                            OpCodes.Call,
                            modifyMethod
                        )
                    }
                );

                Monitor?.Log(
                    "Injected Farming artisan speed modifier.",
                    LogLevel.Trace
                );

                break;
            }
        }

        return codes;
    }

    private static int ModifyTime(
        int minutes,
        MachineData machine,
        Farmer who,
        StardewValley.Object machineObject
    )
    {
        Monitor?.Log(
            $"MODIFYTIME CALLED | " +
            $"ID={machineObject.QualifiedItemId} | " +
            $"Minutes={minutes} | " +
            $"FarmingBase={who.farmingLevel.Value} | " +
            $"FarmingEffective={who.FarmingLevel}",
            LogLevel.Alert
        );

        bool isArtisan =
            IsArtisanMachine(machineObject);

        Monitor?.Log(
            $"MACHINE CHECK | " +
            $"ID={machineObject.QualifiedItemId} | " +
            $"Artisan={isArtisan}",
            LogLevel.Alert
        );

        if (!isArtisan)
            return minutes;

        if (!ModEntry.Config.EnableArtisanSpeed)
        {
            Monitor?.Log(
                "Artisan Speed disabled in config.",
                LogLevel.Alert
            );

            return minutes;
        }

        // Gunakan level Farming dasar, bukan level setelah buff makanan.
        int farmingLevel =
            who.farmingLevel.Value;

        if (farmingLevel < 1)
            return minutes;

        int configLevel =
            Math.Min(
                farmingLevel,
                ModEntry.Config.ArtisanSpeedReduction.Length
            );

        int reduction =
            ModEntry.Config.ArtisanSpeedReduction[
                configLevel - 1
            ];

        Monitor?.Log(
            $"SPEED CHECK | " +
            $"FarmingBase={farmingLevel} | " +
            $"ConfigLevel={configLevel} | " +
            $"Reduction={reduction}%",
            LogLevel.Alert
        );

        if (reduction <= 0)
            return minutes;

        int newMinutes =
            Math.Max(
                1,
                minutes - (minutes * reduction / 100)
            );

        Monitor?.Log(
            $"ARTISAN SPEED APPLIED | " +
            $"{minutes} -> {newMinutes} min | " +
            $"-{reduction}%",
            LogLevel.Alert
        );

        return newMinutes;
    }

    private static bool IsArtisanMachine(
        StardewValley.Object machine
    )
    {
        string id =
            machine.QualifiedItemId;

        return id switch
        {
            // Vanilla
            "(BC)12" => true,
            "(BC)15" => true,
            "(BC)16" => true,
            "(BC)17" => true,
            "(BC)19" => true,
            "(BC)24" => true,
            "(BC)Dehydrator" => true,
            "(BC)FishSmoker" => true,

            // Cornucopia Artisan Machines
            "(BC)Cornucopia_Alembic" => true,
            "(BC)Cornucopia_ButterChurn" => true,
            "(BC)Cornucopia_DryingRack" => true,
            "(BC)Cornucopia_Extruder" => true,
            "(BC)Cornucopia_CompactMill" => true,
            "(BC)Cornucopia_DeluxeSmoker" => true,
            "(BC)Cornucopia_WaxBarrel" => true,
            "(BC)Cornucopia_YogurtJar" => true,
            "(BC)Cornucopia_VinegarKeg" => true,
            "(BC)Cornucopia_Juicer" => true,

            _ => false
        };
    }
}
