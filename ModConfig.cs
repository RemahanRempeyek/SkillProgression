namespace SkillProgression;

public sealed class ModConfig
{
    public int[] MaxHealthBonuses { get; set; } =
        new int[10]
        {
            10, 20, 30, 40, 50,
            65, 80, 90, 100, 110
        };

    public int[] DefenseBonuses { get; set; } =
        new int[10]
        {
            1, 2, 3, 4, 5,
            6, 7, 8, 9, 10
        };

    public bool EnableFarmingQualityBoost { get; set; } = true;

    public int[] FarmingQualityNormalToSilver { get; set; } =
        new int[10]
        {
            0, 0, 0, 0,
            5, 10, 15, 20, 25, 35
        };

    public int[] FarmingQualitySilverToGold { get; set; } =
        new int[10]
        {
            0, 0, 0, 0,
            0, 5, 10, 15, 20, 25
        };

    public int[] FarmingQualityGoldToIridium { get; set; } =
        new int[10]
        {
            0, 0, 0, 0,
            0, 0, 0, 5, 10, 15
        };

    public bool EnableHarvestQuantityBoost { get; set; } = true;

    public int[] HarvestQuantityChance { get; set; } =
        new int[10]
        {
            0, 0, 0, 0,
            10, 15, 20, 25, 30, 40
        };

    public bool EnableArtisanSpeed { get; set; } = true;

    public int[] ArtisanSpeedReduction { get; set; } =
        new int[10]
        {
            0, 0, 0, 0,
            5, 10, 20, 30, 40, 50
        };
}
