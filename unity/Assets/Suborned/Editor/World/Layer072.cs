namespace Suborned.World
{
    /// <summary>Brief 072-E: the batch layers the every-face pass adds beside `buildings` and `skins` (Combine keys, batches/&lt;layer&gt;).
    /// `-every`: the procedural wall that is left over the modules on a wall they stand on (drawn with the modules). `-today`: that wall as it
    /// was before the modules (only the wave-2 street pieces cover it), built off, for the perf harness's everyFace A/B (development builds;
    /// a release build strips it). `-shell`: plain shadows-only walls behind the modules.</summary>
    public static class Layer072
    {
        public const string BuildingsEvery = "buildings-every", BuildingsToday = "buildings-today", SkinsEvery = "skins-every", SkinsToday = "skins-today", Shell = "buildings-shell";
        public static readonly string[] Every = { BuildingsEvery, SkinsEvery }, Today = { BuildingsToday, SkinsToday };
    }
}
