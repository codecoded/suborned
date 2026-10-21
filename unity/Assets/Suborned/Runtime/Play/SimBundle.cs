using System.Collections.Generic;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Suborned.Sim;
using UnityEngine;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>The engine's inputs as Unity loads them (brief 011 D): the Suborned.Sim DLL (Assets/Suborned/Sim/Plugins) and the four texts it
    /// reads, from StreamingAssets/sim/, both written by `bun tools/sim/unity.ts` from one commit. Only texts bun validated load (Redline E1:
    /// Engine.FromTexts lets unknown keys through and a bad text can overflow the stack): each file's sha256 must equal the one engine.json
    /// recorded after bun's schemas and the headless CLI accepted it; anything else is refused before the engine sees it.</summary>
    public static class SimBundle
    {
        public static string Dir => Path.Combine(Application.streamingAssetsPath, "sim");
        /// <summary>Brief 013 E: the provisional facts (security level 2) and the world's takedown places (stashes, service doors, call boxes,
        /// posts, watchers) load too, or the engine runs at level 1 with no stashes and no record matches headless.</summary>
        public static readonly string[] Files = { "rulebook.json", "people.rules.json", "handover.site.json", "handover.people.json", "handover.provisional.json", "handover.takedown.json", "handover.rounds.json" }; // brief 022: Inez's rounds (without them the host plays none: a split from headless)

        public sealed class Loaded
        {
            public Engine.Inputs Inputs;
            public string Commit = "", Source = "", DllSha = "";
            public JNode Manifest;
        }

        /// <summary>Reads engine.json and the six inputs, checks every hash, and builds the engine's inputs. Throws on any mismatch.</summary>
        public static Loaded Load()
        {
            var manifestPath = Path.Combine(Dir, "engine.json");
            if (!File.Exists(manifestPath)) throw new FileNotFoundException($"Sim: {manifestPath} missing; run `bun tools/sim/unity.ts`");
            var m = JNode.Parse(File.ReadAllText(manifestPath), "engine.json");
            if (m["schema"].Str() != "suborned.sim.unity/1") throw new InvalidDataException("Sim: engine.json is not suborned.sim.unity/1");
            var texts = new string[Files.Length];
            for (int i = 0; i < Files.Length; i++)
            {
                var path = Path.Combine(Dir, Files[i]);
                var bytes = File.ReadAllBytes(path);
                var want = m["inputs"][Files[i]].Str();
                var got = Sha(bytes);
                if (got != want) throw new InvalidDataException($"Sim: {Files[i]} sha256 {got[..12]} is not the validated {want[..12]} (engine.json); run `bun tools/sim/unity.ts`");
                texts[i] = new UTF8Encoding(false).GetString(bytes);
            }
            return new Loaded
            {
                Inputs = Engine.FromTexts(texts[0], texts[1], texts[2], texts[3], texts[4], texts[5], texts[6]),
                Commit = m["commit"].Str(), Source = m["source"].Str(), DllSha = m["dllSha256"].Str(), Manifest = m,
            };
        }

        static readonly Dictionary<string, Engine.Inputs> range = new();
        /// <summary>Brief 029: the fight range's inputs for a layout (open, dense): the rulebook and people rules (engine.json `inputs`) and the
        /// range site (engine.json `range`), every hash checked as Load checks the Handover's. Throws when the bundle carries no range.</summary>
        public static Engine.Inputs LoadRange(string layout)
        {
            if (range.TryGetValue(layout, out var have)) return have;
            var site = layout == "dense" ? "range-dense.site.json" : layout == "open" ? "range.site.json" : layout == "lane" ? "range-lane.site.json" : throw new InvalidDataException($"Sim: range layout '{layout}' is not open, dense or lane"); // lane: engine 0.16 (tuning.md §10.4)
            var m = JNode.Parse(File.ReadAllText(Path.Combine(Dir, "engine.json")), "engine.json");
            if (m.Opt("range") is not JNode rb || rb.Opt(site) is not JNode want) throw new InvalidDataException($"Sim: engine.json has no range {site}; run `bun tools/sim/unity.ts --tree`");
            string Read(string file, string sha)
            {
                var bytes = File.ReadAllBytes(Path.Combine(Dir, file)); var got = Sha(bytes);
                if (got != sha) throw new InvalidDataException($"Sim: {file} sha256 {got[..12]} is not the validated {sha[..12]} (engine.json); run `bun tools/sim/unity.ts`");
                return new UTF8Encoding(false).GetString(bytes);
            }
            var inputs = Engine.FromTexts(Read("rulebook.json", m["inputs"]["rulebook.json"].Str()), Read("people.rules.json", m["inputs"]["people.rules.json"].Str()), Read(site, want.Str()));
            range[layout] = inputs; return inputs;
        }

        /// <summary>headless.json: every scenario as the CLI played it with the same DLL and inputs.</summary>
        public static JNode Headless() => JNode.Parse(File.ReadAllText(Path.Combine(Dir, "headless.json")), "headless.json");

        public static string Sha(byte[] b) { using var h = SHA256.Create(); var sb = new StringBuilder(64); foreach (var x in h.ComputeHash(b)) sb.Append(x.ToString("x2")); return sb.ToString(); }

        /// <summary>Which scripting runtime runs this code.</summary>
        public static string Runtime =>
#if ENABLE_IL2CPP
            "IL2CPP";
#else
            "Mono";
#endif
    }
}
