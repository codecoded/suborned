using System.IO;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Stop stills for look review (Glint): one PNG per flyby stop at the stop's mid-hold, 960x540, into
    /// out/shots/glint2/&lt;tag&gt;/ (tag = "before" or "after"). Never touches out/shots/flyby. Menu: Suborned/World/Glint Stills.</summary>
    public static class GlintStills
    {
        [MenuItem("Suborned/World/Glint Stills (before)")] public static void Before() => Run("before");
        [MenuItem("Suborned/World/Glint Stills (after)")] public static void After() => Run("after");

        static void Run(string tag) => HarbourWorldBuilder.Batch(() =>
        {
            if (!RenderSession.EnsureScene()) return;
            var path = Flyby.Load();
            var dir = Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "shots", "glint2", tag);
            Directory.CreateDirectory(dir);
            using var session = new RenderSession();
            var cam = RenderSession.TempCamera("glint-stills");
            try
            {
                path.Evaluate(0f).Apply(cam);
                session.Grab(cam, 64, 64);
                for (int i = 0; i < path.Keys.Count; i++)
                {
                    var k = path.Keys[i];
                    if (!k.Contact) continue;
                    var pose = path.Evaluate(k.Start + k.Hold * 0.5f);
                    pose.Apply(cam); session.ShadowDistance = pose.ShadowDistance;
                    File.WriteAllBytes(Path.Combine(dir, $"stop-{i:D2}-{Slug(k.Name)}.png"), session.Grab(cam, 960, 540).EncodeToPNG());
                }
                Debug.Log($"World: glint stills {tag} -> {dir}");
            }
            finally { Object.DestroyImmediate(cam.gameObject); }
        });

        static string Slug(string s) { var o = new System.Text.StringBuilder(); foreach (var c in s.ToLowerInvariant()) o.Append(char.IsLetterOrDigit(c) ? c : '-'); return o.ToString(); }
    }
}
