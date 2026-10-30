using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 084 (Sky 2026-10-31: "Let's try glyphs recommendations"): the fight cues as lab switches, each between as built and Glyph's
    /// pick, and the values Sky tunes live. A try, not a ruling (GDD §11 playground). View only: no switch or value reaches the engine, so a run
    /// replays the same with any of them. The range opens on the pick; everywhere else, and in any proof, everything stays as built until a
    /// switch is thrown (the P panel in the Handover).</summary>
    public static class FightCues
    {
        public enum ShotLook { AsBuilt, Hot, HotJolt }
        public static bool Mine, Target, Incoming, Edge, Blind; public static ShotLook Shots;
        public static bool Hot => Shots != ShotLook.AsBuilt;
        public static bool Jolt => Shots == ShotLook.HotJolt;
        public static bool Any => Mine || Target || Incoming || Edge || Blind || Shots != ShotLook.AsBuilt;
        public static bool AllPick => Mine && Target && Incoming && Edge && Blind && Shots == ShotLook.HotJolt;

        /// <summary>The values Sky reads off. Pixels are at play zoom (10) and scale with the zoom.</summary>
        [Serializable]
        public sealed class Values
        {
            public float flashSize = 1f, flashTime = 0.16f, sparkTime = 0.3f, knockPx = 15f, knockTime = 0.5f, whiteTime = 0.12f, kickPx = 5f;
            public bool paving = true; /** the grey floor's tint that lands on the street's lit paving as drawn in play (241, 228, 199 measured off the shot) */ public float floorR = 1.383f, floorG = 1.22f, floorB = 1.07f;
            /// <summary>One line, to paste back.</summary>
            public string Line() => $"flash size {flashSize:0.00} · flash time {flashTime:0.00} s · spark time {sparkTime:0.00} s · knock {knockPx:0.0} px · knock time {knockTime:0.00} s · white {whiteTime:0.00} s · kick {kickPx:0.0} px";
        }
        public static Values V = new();
        /// <summary>A floor change waiting for RangePieces.</summary>
        public static int FloorVersion;

        public static void SetAll(bool pick) { Mine = Target = Incoming = Edge = Blind = pick; Shots = pick ? ShotLook.HotJolt : ShotLook.AsBuilt; }

        /// <summary>A person's run, or `-debug` (which the release player needs to show the range at all): any other flag is a proof, which stays as
        /// built. GameBoot.Automation counts `-debug` as automation, so its rule is repeated here without that flag.</summary>
        public static bool LabRun => ForceLab || (labRun ??= !Application.isBatchMode && !Environment.GetCommandLineArgs().Skip(1).Any(x => x.StartsWith("-") && !LaunchFlag(x)));
        static bool? labRun;
        /// <summary>The cue proof's own run (ProofCues).</summary>
        public static bool ForceLab;
        static bool LaunchFlag(string x) => x is "-debug" or "-windowed" or "-popupwindow" or "-window-mode" or "-monitor" or "-ApplePersistenceIgnoreState" || x.StartsWith("-screen-") || x.StartsWith("-psn_") || x.StartsWith("-NS") || x.StartsWith("-Apple");

        static int scene = -1;
        /// <summary>Per frame from the HUD: a scene change sets the opening state: the range on the pick (a person's run), else as built. A restart keeps the switches.</summary>
        public static void Sync(bool range)
        {
            int now = range ? 1 : 0; if (scene == now) return; scene = now;
            SetAll(range && LabRun); if (LabRun) Load(); else V = new Values(); FloorVersion++;
        }

        /// <summary>Where the values live: the player's persistent data folder.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, "fight-cues.json");
        public static void Save() { if (!LabRun) return; try { File.WriteAllText(FilePath, JsonUtility.ToJson(V, true)); } catch (Exception e) { Debug.LogWarning($"FightCues: {e.Message}"); } }
        static void Load() { try { if (File.Exists(FilePath) && !ForceLab) JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), V); } catch (Exception e) { Debug.LogWarning($"FightCues: {e.Message}"); } }
        /// <summary>Back to Glyph's numbers.</summary>
        public static void Reset() { V = new Values(); FloorVersion++; Save(); }

        /// <summary>The cues' own time this run, for the frame cost (ProofCues): Stopwatch ticks and frames while any switch is on.</summary>
        public static long CostTicks; public static int CostFrames;
        public static float CostMs => CostFrames > 0 ? (float)(CostTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency / CostFrames) : 0f;
    }
}
