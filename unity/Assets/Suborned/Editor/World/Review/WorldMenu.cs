using System.IO;
using UnityEditor;

namespace Suborned.World.Review
{
    /// <summary>Suborned/World/Build Harbour District (Export): builds the scene from the generated export
    /// (out/world/harbour-district.world.json, `bun run world`) rather than the sample.</summary>
    public static class WorldMenu
    {
        [MenuItem("Suborned/World/Build Harbour District (Export)")]
        public static void BuildExport() => HarbourWorldBuilder.Batch(() =>
        {
            var path = RenderSession.ExportPath;
            if (!File.Exists(path)) throw new FileNotFoundException($"World: {path} missing; run `bun run world`");
            if (HarbourWorldBuilder.MayReplaceScene()) HarbourWorldBuilder.BuildFrom(path);
        });
    }
}
