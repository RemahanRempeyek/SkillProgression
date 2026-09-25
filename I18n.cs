using StardewModdingAPI;

namespace SkillProgression;

internal static class I18n
{
    private static ITranslationHelper? Translation;

    public static void Init(ITranslationHelper translation)
    {
        Translation = translation;
    }

    public static string Get(
        string key,
        object? tokens = null)
    {
        if (Translation == null)
            return key;

        return Translation
            .Get(key, tokens)
            .ToString();
    }
}
