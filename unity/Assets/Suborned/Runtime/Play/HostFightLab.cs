using System;
using System.Collections.Generic;
using System.IO;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using Path = System.IO.Path;

namespace Suborned.Play
{
    /// <summary>Brief 027's fight playground switches on the host side (briefs/027/playground.md §1, §2; Keel's host-interface.md), EXPERIMENTAL.
    /// A preset (briefs/027/presets/today.json, lab.json) has a host part (these fields) and a start part (the engine's fightLab, which applies
    /// from a restart, as brief 025's drive switches do). The mode switches live: realtime, freeze (Desperados-style plan view), target
    /// (VATS-style volleys). A checkpoint (briefs/027/checkpoints/cp-loud, cp-safe) is a start and its orders, played to its tick on restart.</summary>
    [Serializable]
    public sealed class FightLab
    {
        public string preset = "today", fightMode = "realtime", checkpoint = "";
        public bool pauseInFight = true;
        public bool onLoud = true, onSpotted = true, onFiredOn = true, onAmbush = true, onReinforce = true, onDown = true, onBeat = false;
        public float freezeMinGapS = 3f; public int freezesPerFight = -1; // -1 unlimited (caps manual freezes only)
        public bool tAmbush = true, tFiredOn = true, tDown = true; public float targetPlayback = 1f;
        /// <summary>Glyph's finished fight HUD (briefs/027/glyph/hud.html §3): each open choice is a switch, Glyph's pick the default.</summary>
        public FightHudLook Hud = new();
        [NonSerialized] public JNode Start; // the preset's start part (null: lab off)
        [NonSerialized] public string LoadError = "";

        /// <summary>briefs/027/&lt;sub&gt; in the repo: up from the Editor's Assets, or from the built app's Contents (out/build/play/The Suborned.app/Contents:
        /// the first player run of the shot runner, 2026-10-21, looked in out/build/play/briefs and loaded no checkpoint).</summary>
        public static string Dir(string sub)
        {
            var d = new DirectoryInfo(Application.dataPath);
            for (int i = 0; i < 7 && d != null; i++, d = d.Parent) { var c = Path.Combine(d.FullName, "briefs", "027"); if (Directory.Exists(c)) return Path.Combine(c, sub); }
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "briefs", "027", sub));
        }
        public static readonly string[] Presets = { "today", "lab" }, Modes = { "realtime", "freeze", "target" }, Checkpoints = { "", "cp-loud", "cp-safe" };

        /// <summary>Reads briefs/027/presets/&lt;name&gt;.json (suborned.fightlab.preset/1): its host part into these fields, its start part kept for the next restart.</summary>
        public void Load(string name)
        {
            LoadError = "";
            var f = Path.Combine(Dir("presets"), name + ".json");
            try
            {
                var n = JNode.Parse(File.ReadAllText(f), name + ".json");
                if (n["schema"].Str() != "suborned.fightlab.preset/1") throw new FormatException("schema is not suborned.fightlab.preset/1");
                var h = n["host"]; preset = name;
                fightMode = h["fightMode"].Str(); pauseInFight = h["pauseInFight"].Bool();
                var fo = h["freezeOn"]; onLoud = fo["loudOrder"].Bool(); onSpotted = fo["spotted"].Bool(); onFiredOn = fo["firedOn"].Bool(); onAmbush = fo["ambush"].Bool();
                onReinforce = fo["reinforce"].Bool(); onDown = fo["down"].Bool(); onBeat = fo["beat"].Bool();
                freezeMinGapS = h["freezeMinGapS"].Int(); freezesPerFight = h.Opt("freezesPerFight") is JNode fp && fp.Kind == JKind.Num ? fp.Int() : -1;
                tAmbush = tFiredOn = tDown = false; foreach (var t in h["targetAuto"].Items) switch (t.Str()) { case "ambush": tAmbush = true; break; case "firedOn": tFiredOn = true; break; case "down": tDown = true; break; }
                targetPlayback = h.Opt("targetPlayback") is JNode tp ? float.Parse(tp.NumberText, System.Globalization.CultureInfo.InvariantCulture) : 1f;
                Start = n["start"];
            }
            catch (Exception e) { LoadError = $"{Path.GetFileName(f)}: {e.Message}"; Debug.LogWarning($"FightLab: {LoadError}"); }
        }
    }

    /// <summary>Glyph's §3 choices for the fight HUD (EXPERIMENTAL; P panel). Defaults = Glyph's picks. Presentation only: no switch changes a run.</summary>
    [Serializable]
    public sealed class FightHudLook
    {
        public bool creamFill = true;        // #1 health fill cream with a side-coloured frame (off: filled in the side colour)
        public bool ladder = true;           // #2 freeze queue as markers on the roster (off: the debug strip across the top)
        public bool trailAltOnly = true;     // #3 Trail on the hit chip only while Alt is held (off: always)
        public bool rimOnly = true;          // #4 freeze look: the turquoise rim only (off: the world also desaturates, signals stay outside the grade)
        public bool keysOnly = true;         // #5 keycaps only on the mode pill (off: a word beside each key)
        public bool chargeOnPortrait = true; // #6 charge as a turquoise bar under the portrait's health (off: no charge bar)
        public bool hatched = true;          // previewed damage hatched (off: a solid pale chunk)
        public bool arrows = true;           // hostile focus lines with arrowheads (off: no heads)
        public bool debugLayer = false;      // the B0–B2 IMGUI debug layer on top (for comparison)
    }

    public sealed partial class HandoverHost
    {
        public FightLab Fights { get; } = new();
        bool fightLabLoaded;
        /// <summary>What the run being played started with (preset, checkpoint): the P panel offers a restart when the switches differ.</summary>
        public string FightRunPreset { get; private set; } = "today";
        public string FightRunCheckpoint { get; private set; } = "";
        public bool FightLabPending => Game != null && (Fights.preset != FightRunPreset || Fights.checkpoint != FightRunCheckpoint);

        static void CopyLab(Suborned.Sim.Start from, Suborned.Sim.Start to)
        {
            to.FightLabOn = from.FightLabOn; to.LabHealthScale = from.LabHealthScale; to.LabPips = from.LabPips; to.LabClose = from.LabClose; to.LabCharge = from.LabCharge;
            to.LabField = from.LabField; to.LabWarn = from.LabWarn; to.LabFireMoving = from.LabFireMoving; to.LabStances = from.LabStances; to.LabThen = from.LabThen;
            for (int k = 0; k < 4; k++) to.LabLoadout[k] = from.LabLoadout[k];
        }
        static void CopyDrive(Suborned.Sim.Start from, Suborned.Sim.Start to) { to.DrivePace = from.DrivePace; to.DriveGap = from.DriveGap; to.DriveOnBusy = from.DriveOnBusy; to.DriveOnRelease = from.DriveOnRelease; }

        /// <summary>The run's game: the drive switches (brief 025) and the fight preset's start part; with a checkpoint, its start and its orders
        /// played to its tick (no Finish: an open fight stays open), so the player takes over there. The orders are the session's own, so the saved
        /// session replays headless from tick 0.</summary>
        /// <summary>A proof's Arms for the next Begin (item 13's done-check: Arms 1 arms the Scout and the Enforcer only); null: the mission's.</summary>
        public static int? ArmsOverride;
        Game NewFightGame()
        {
            if (ContinueGame() is Game cg) return cg; // brief 051: Continue (a saved session replayed)
            if (!fightLabLoaded) { fightLabLoaded = true; Fights.Load(Fights.preset); }
            var lab = new Suborned.Sim.Start(); if (Fights.Start != null) CopyLab(Suborned.Sim.Start.Parse(Fights.Start), lab);
            FightRunPreset = Fights.preset; FightRunCheckpoint = Fights.checkpoint;
            if (Fights.checkpoint.Length == 0 || rangeMode) // brief 029: the range has no checkpoints
            {
                var s = Lab.ToStart(); CopyLab(lab, s); if (ArmsOverride is int ar) s.Arms = ar;
                return Engine.New(PlayInputs, s);
            }
            var sc = Script.Parse(File.ReadAllText(Path.Combine(FightLab.Dir("checkpoints"), Fights.checkpoint + ".script.json")));
            CopyDrive(Lab.ToStart(), sc.Start); CopyLab(lab, sc.Start); // the checkpoint carries no lab fields: the preset's (Keel)
            var g = Engine.New(Bundle.Inputs, sc.Start);
            var list = new List<(int i, Order o)>(); for (int i = 0; i < sc.Orders.Count; i++) list.Add((i, sc.Orders[i]));
            list.Sort((a, b) => a.o.Tick != b.o.Tick ? a.o.Tick.CompareTo(b.o.Tick) : a.i.CompareTo(b.i));
            foreach (var (_, o) in list) { g.RunTo(o.Tick); if (g.Over) break; g.Issue(o); }
            g.RunTo(sc.Until);
            lastLog = g.Log.Count; // the checkpoint's lines are history: no pops for them
            Debug.Log($"FightLab: {Fights.checkpoint} loaded at {Report.Clock(g.Tick)} (preset {Fights.preset}, lab {(g.Lab ? "on" : "off")}, fight {(g.FightOn ? "on" : "off")})");
            return g;
        }

        // ---------------------------------------------------------------- freeze and target mode (host time only, playground.md §0)

        /// <summary>The clock is held by a freeze (no ticks run; the camera, hover and the scope stay live).</summary>
        public bool Frozen { get; private set; }
        /// <summary>The freeze is target mode's: clicks on combatants build volleys.</summary>
        public bool TargetFreeze { get; private set; }
        public string FrozenWhy { get; private set; } = "";
        public readonly Order[] Queued = new Order[4]; public Order QueuedCell { get; private set; }
        public readonly string[] VolleyTarget = new string[4]; public readonly int[] VolleyShots = new int[4];
        int lastReleaseTick = -100000, manualFreezes; bool releasing;
        public int FreezeCount { get; private set; }
        public int VolleyCount { get; private set; }
        float wallStart;

        void FightLabReset()
        {
            Frozen = false; TargetFreeze = false; FrozenWhy = ""; QueuedCell = null; Array.Clear(Queued, 0, 4); Array.Clear(VolleyTarget, 0, 4); Array.Clear(VolleyShots, 0, 4);
            lastReleaseTick = -100000; manualFreezes = 0; FreezeCount = 0; VolleyCount = 0; wallStart = Time.unscaledTime;
        }

        bool FreezeModes => Fights.fightMode is "freeze" or "target";
        /// <summary>A volley given at the last release still fires: the host may slow its own tick rate (targetPlayback; presentation only).</summary>
        float PlaybackScale { get { if (Game == null || !Game.Lab || Fights.targetPlayback >= 0.999f) return 1f; foreach (var op in Game.Ops) if (op.Gun.VolleyLeft > 0) return Mathf.Clamp(Fights.targetPlayback, 0.25f, 1f); return 1f; } }

        public void EnterFreeze(string why, bool target, bool manual)
        {
            if (Game == null || Game.Over || Fault != null) return;
            if (Frozen) { if (target && !TargetFreeze) { TargetFreeze = true; FrozenWhy = why; } return; }
            if (manual && Fights.freezesPerFight >= 0 && manualFreezes >= Fights.freezesPerFight) { Pop($"No freezes left this fight ({Fights.freezesPerFight})", null, SelectedWorld(), Feedback.Refused, 2f, "", ""); return; }
            if (Driving) DriveToggle(); // §1.4: the drive is let go (DriveRelease, this tick) before any freeze; no freeze while driven
            Frozen = true; TargetFreeze = target; FrozenWhy = why; FreezeCount++; if (manual) manualFreezes++; acc = 0f;
            QueuedCell = null; Array.Clear(Queued, 0, 4); Array.Clear(VolleyTarget, 0, 4); Array.Clear(VolleyShots, 0, 4);
            Status = target ? $"Target mode ({why}): click a combatant to add a shot · right click removes one · {controls.Label(PlayAction.TargetMode)} or Return fires · Esc leaves"
                            : $"Frozen ({why}): give each agent one action · Return releases · Esc or Space leaves without orders";
            Save();
        }

        /// <summary>Leave the freeze without giving the queue.</summary>
        public void LeaveFreeze()
        {
            if (!Frozen) return;
            Frozen = false; TargetFreeze = false; QueuedCell = null; Array.Clear(Queued, 0, 4); Array.Clear(VolleyTarget, 0, 4); Array.Clear(VolleyShots, 0, 4);
            lastReleaseTick = Game.Tick; Status = "Running.";
        }

        /// <summary>Release: every queued order on the frozen tick, the cell's first, then the agents in order (scout, fixer, tech, enforcer; a
        /// driven agent first, after its DriveRelease, though a freeze has already let any drive go), then the volleys; the clock runs again.</summary>
        public void Release()
        {
            if (!Frozen || Game == null) return;
            releasing = true;
            try
            {
                int first = DriveAgent; if (first >= 0) { DriveRelease(first); }
                if (QueuedCell != null) Issue(QueuedCell);
                var order = new List<int>(); if (first >= 0) order.Add(first); for (int k = 0; k < 4; k++) if (k != first) order.Add(k);
                foreach (int k in order)
                {
                    if (Queued[k] != null) Issue(Queued[k]);
                    if (VolleyShots[k] > 0 && !string.IsNullOrEmpty(VolleyTarget[k]))
                        if (Issue(new Order { Op = Op.Name((OpId)k), Do = "volley", Target = VolleyTarget[k], Shots = VolleyShots[k] })) VolleyCount++;
                }
            }
            finally { releasing = false; }
            int n = 0; for (int k = 0; k < 4; k++) if (Queued[k] != null || VolleyShots[k] > 0) n++;
            LeaveFreeze(); Paused = false; ShowBriefing = false; if (ClockStarted < 0) ClockStarted = Time.unscaledTime;
            Status = $"Released: {n + (QueuedCell != null ? 1 : 0)} order{(n == 1 ? "" : "s")} on {Report.Clock(Game.Tick)}";
        }

        /// <summary>In a freeze an agent's order is held, not given: one action per agent (a new one replaces it); the cell's (panic) its own slot.
        /// Drive samples and releases are never held (§1.4).</summary>
        bool QueueOrder(Order o, Vector2? screen, Vector3? target)
        {
            if (!Frozen || releasing || o.Do == "drive" || o.Release) return false;
            var q = new Order { Op = o.Op, Do = o.Do, Target = o.Target, Approach = o.Approach, To = o.To, Stance = o.Stance, How = o.How, Point = o.Point, Run = o.Run, Force = o.Force, Shots = o.Shots, Then = o.Then };
            if (o.Op == "cell") QueuedCell = q;
            else
            {
                int k = Array.IndexOf(new[] { "scout", "fixer", "tech", "enforcer" }, o.Op); if (k < 0) return false;
                Queued[k] = q; VolleyShots[k] = 0; VolleyTarget[k] = null; // one action per agent: a click replaces its volley too
            }
            Pop($"{o.Op}: {o.Do} {(o.Target.Length > 0 ? o.Target : o.To)} · queued", screen, screen.HasValue ? null : target, Feedback.Ok, 1.5f, "", "");
            return true;
        }

        /// <summary>After a step: the first freeze trigger in this step's log lines (playground.md §1.2's table; codes the engine already logs).
        /// Freeze mode: the freezeOn switches; target mode: the targetAuto ones. Not within freezeMinGapS of a release; never while driven (the
        /// status flashes the trigger instead).</summary>
        void FreezeScan(int from)
        {
            if (!FreezeModes || Frozen || Game.Over) return;
            bool target = Fights.fightMode == "target";
            for (int i = from; i < Game.Log.Count; i++)
            {
                var l = Game.Log[i]; string trig = null;
                if (l.Code == "drawn.seen") trig = "spotted";
                else if (l.Code.StartsWith("shot.") && l.Text.IndexOf(" fires on ", StringComparison.Ordinal) is int f && f > 0)
                { int a = l.Text.IndexOf(" at ", f + 1, StringComparison.Ordinal); string t = a > f ? l.Text[(f + 10)..a] : ""; if (t is "scout" or "fixer" or "tech" or "enforcer") trig = "fired on"; }
                else if (l.Code is "fight.ambush" or "fight.loud") trig = "the fight springs";
                else if (l.Code == "response.reinforces") trig = "reinforcements";
                else if (l.Code is "scout.down" or "fixer.down" or "tech.down" or "enforcer.down") trig = "one of the cell down";
                if (trig == null) continue;
                bool on = target ? trig switch { "the fight springs" => Fights.tAmbush, "fired on" => Fights.tFiredOn, "one of the cell down" => Fights.tDown, _ => false }
                                 : trig switch { "spotted" => Fights.onSpotted, "fired on" => Fights.onFiredOn, "the fight springs" => Fights.onAmbush, "reinforcements" => Fights.onReinforce, "one of the cell down" => Fights.onDown, _ => false };
                if (!on) continue;
                if (Game.Tick - lastReleaseTick < Fights.freezeMinGapS * Game.Hz) continue;
                if (Driving) { Status = $"⚑ {trig} (driven: no freeze; T lets go)"; Pop($"⚑ {trig}", null, SelectedWorld(), Feedback.Refused, 1.5f, "", ""); return; }
                EnterFreeze(trig, target, false); return;
            }
        }

        /// <summary>The going-loud card would open: in freeze mode with freezeOn.loudOrder, the freeze opens first and the card shows inside it.</summary>
        void FreezeOnLoud() { if (Fights.fightMode == "freeze" && Fights.onLoud && !Frozen && !Driving) EnterFreeze("going loud", false, false); }

        /// <summary>Space with the lab's modes: in a freeze, leaves it; in freeze or target mode during a fight, freezes (manual); in realtime with
        /// pauseInFight off, a fight doesn't pause. True when the key was used here (else it is the pause key as always).</summary>
        bool FightLabSpace()
        {
            if (Frozen) { LeaveFreeze(); return true; }
            if (Game == null || Paused || !Game.FightOn) return false;
            if (FreezeModes) { EnterFreeze("manual", Fights.fightMode == "target", true); return true; }
            if (!Fights.pauseInFight) { Status = "Pure real time: no pause while a fight is on (P panel: pause in fight)"; return true; }
            return false;
        }

        /// <summary>The Target key (Y): target mode opens (any fight mode, to try it); in target mode it fires the volleys.</summary>
        void TargetKey()
        {
            if (Frozen && TargetFreeze) { Release(); return; }
            if (Game == null || Game.Over) return;
            if (!Game.Lab) { Pop("Target mode needs the lab preset (P panel)", null, SelectedWorld(), Feedback.Refused, 2f, "", ""); return; }
            if (Frozen) { TargetFreeze = true; FrozenWhy = "target"; return; }
            EnterFreeze("target", true, true);
        }

        /// <summary>Target mode's clicks: left on a combatant adds a shot to the selected agent's volley (a new target starts it again), right
        /// removes one. Bounded by target.maxShots and what the agent's charge pays (PreviewVolley refuses past it). True when used.</summary>
        bool VolleyClick(Vector2 p, bool left, bool right)
        {
            if (!TargetFreeze || (!left && !right)) return false;
            var t = TargetAt(p); if (t == null) return false;
            bool ctrl = Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
            if (!ShotAt(t, ctrl, out var id, out _)) return false;
            if (id == "security") id = FirstOfficer();
            int k = (int)Agent; if (!Selected[k] || !Armed(k)) { k = -1; for (int i = 0; i < 4; i++) if (Selected[i] && Armed(i)) { k = i; break; } }
            if (k < 0) { Pop("Select an armed agent first", p, null, Feedback.Refused, 2f, "", ""); return true; }
            int shots = VolleyTarget[k] == id ? VolleyShots[k] : 0;
            if (right) { if (shots > 0) { VolleyShots[k] = shots - 1; if (VolleyShots[k] == 0) VolleyTarget[k] = null; } return true; }
            VolleyPreview v = null; if (!Guard("volley", () => v = Game.PreviewVolley((OpId)k, id, shots + 1))) return true;
            if (v.Refused.Length > 0) { Pop($"{AgentNames[k]}: {v.Refused}", p, null, Feedback.Refused, 2f, "", ""); return true; }
            VolleyTarget[k] = id; VolleyShots[k] = shots + 1; Queued[k] = null; // the volley is this agent's one action
            return true;
        }

        /// <summary>Health as the bars draw it: with the lab, one segment per 10 (host-interface: "one segment is 10"); else today's points.</summary>
        public int Seg(int hp) => Game != null && Game.Lab ? (hp + 9) / 10 : hp;

        /// <summary>Lab chip (B1): pips · −damage · ×hits from PreviewFire, no seconds; null when the lab is off (today's chip).</summary>
        string LabAim(int k, string id, bool force, Target t, AimParts parts)
        {
            if (!Game.Lab) return null;
            FirePreview p = Game.PreviewFire((OpId)k, id, force);
            string who = force ? $"CIVILIAN · {Name(id)}" : Cap(Name(id));
            if (p.Refused.Length > 0) { parts.Refused = p.Refused.Replace('-', ' '); return $"{who} · {p.Refused}"; }
            string pips = new string('●', Mathf.Clamp(p.Pips, 0, 4)) + new string('○', 4 - Mathf.Clamp(p.Pips, 0, 4)); // Keel: with Walk the pips are read from the stop point
            parts.Lab = true; parts.Down = $"{pips} −{p.Damage} ×{p.HitsToDown}"; parts.Ratio = $"{p.Acc / 10}%";
            int tr = p.Untraced ? 0 : p.TrailShooting + p.TrailHit; parts.Trail = tr > 0 ? $"+{tr} T" : "";
            LabChunk = (id, p.Health, p.Max, p.Damage); LabFire = p; LabFireTarget = id; LabFireAgent = k;
            parts.WalkS = p.Walk ? WalkSeconds(p.WalkTicks) : -1; parts.Charge = p.ChargePerShot; parts.Band = p.Band; parts.Exposed = p.Walk ? p.WalkExposedTo.Count : 0;
            string walk = p.Walk ? $" · walks in {WalkSeconds(p.WalkTicks)} s" : "";
            string charge = p.ChargePerShot > 0 ? $" · ▮{p.ChargePerShot}" : "";
            return $"{who} · {pips} −{p.Damage} ×{p.HitsToDown}{charge}{walk} · {p.DistanceCm / 100} m {p.Band}{(p.Cover ? " · cover" : "")}{(tr > 0 ? $" · +{tr} T" : "")}";
        }
        /// <summary>Keel's WalkTicks as whole seconds for the chip (rounded up: "🚶 4 s").</summary>
        public static int WalkSeconds(int ticks) => (ticks + Game.Hz - 1) / Game.Hz;
        /// <summary>The hovered target's damage chunk (B1): who, health, max, one hit's damage; drawn by FightLabHud.</summary>
        public (string id, int health, int max, int damage)? LabChunk { get; private set; }
        /// <summary>The hovered target's PreviewFire (the lab chip FightLabHud draws), with whom and by whom.</summary>
        public FirePreview LabFire { get; private set; } public string LabFireTarget { get; private set; } = ""; public int LabFireAgent { get; private set; } = -1;

        // ---------------------------------------------------------------- lines on the signal layer (Glyph's brief 027 hud.html, frame A, D)

        /// <summary>The user layer the signals draw on: the play camera leaves it out, an overlay camera with no post-processing draws it on top
        /// (its depth kept, so buildings still hide a line), so the grade never shifts a signal (GDD §10, §15: signals unlit and emissive).</summary>
        public const int SignalLayer = 8;
        public static readonly Color CellTurquoise = new(0x4f / 255f, 0xe3 / 255f, 0xe8 / 255f), RivalViolet = new(0x8a / 255f, 0x4f / 255f, 0xd0 / 255f), PoliceAmber = new(0xf0 / 255f, 0xa0 / 255f, 0x20 / 255f);
        public static Color SideColour(string id) => id is "scout" or "fixer" or "tech" or "enforcer" ? CellTurquoise : id.StartsWith("rival") ? RivalViolet : PoliceAmber;

        sealed class SigLine { public LineRenderer Body, Head; public float Offset; }
        readonly List<SigLine> sigLines = new(); [NonSerialized] UnityEngine.Camera signalCam; [NonSerialized] Material oursMat, rivalMat, policeMat, headRival, headPolice, headOurs, ghostMat2; static Texture2D dashTex, dotTex;
        [NonSerialized] GameObject ghostRoot; readonly List<(GameObject body, LineRenderer ring)> ghosts = new();
        /// <summary>Badges the HUD draws at our lines' target ends this frame: F-number, shots, world point (chest).</summary>
        public readonly List<(string text, Vector3 at)> LineBadges = new();

        /// <summary>The overlay camera for the signal layer, stacked on the play camera; the play camera stops drawing that layer.</summary>
        void SignalCamera()
        {
            if (!cam) return;
            if (!signalCam)
            {
                var go = new GameObject("signals camera"); go.transform.SetParent(cam.transform, false);
                signalCam = go.AddComponent<UnityEngine.Camera>(); signalCam.cullingMask = 1 << SignalLayer; signalCam.clearFlags = CameraClearFlags.Nothing;
                var d = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                d.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay; d.renderPostProcessing = false; d.renderShadows = false; d.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                var bd = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); if (bd && !bd.cameraStack.Contains(signalCam)) bd.cameraStack.Add(signalCam);
                cam.cullingMask &= ~(1 << SignalLayer);
            }
            signalCam.enabled = true; signalCam.orthographic = cam.orthographic; signalCam.orthographicSize = cam.orthographicSize; signalCam.fieldOfView = cam.fieldOfView;
            signalCam.nearClipPlane = cam.nearClipPlane; signalCam.farClipPlane = cam.farClipPlane;
        }

        static Texture2D Pattern(int on, int off) { var t = new Texture2D(on + off, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point }; var px = new Color32[on + off]; for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(i < on ? 255 : 0)); t.SetPixels32(px); t.Apply(); return t; }

        Material SigMat(Color c, Texture2D pattern)
        {
            var m = new Material(unlitTemplate) { name = "signal" }; m.SetColor("_BaseColor", c); m.renderQueue = 3100;
            if (pattern) { m.SetTexture("_BaseMap", pattern); m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); }
            return m;
        }

        SigLine Line(int i)
        {
            while (sigLines.Count <= i)
            {
                LineRenderer Make(string n)
                {
                    var go = new GameObject(n) { layer = SignalLayer }; go.transform.SetParent(viewRoot, false);
                    var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.positionCount = 2; lr.alignment = LineAlignment.View; lr.numCapVertices = 0;
                    lr.textureMode = LineTextureMode.Tile; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; lr.enabled = false; return lr;
                }
                sigLines.Add(new SigLine { Body = Make("signal-line"), Head = Make("signal-head") });
            }
            return sigLines[i];
        }

        /// <summary>World metres per 1080p pixel at a world point (the play camera's projection): Glyph's sizes are 1080p pixels.</summary>
        float Mpp(Vector3 at)
        {
            if (!cam) return 0.03f;
            if (cam.orthographic) return 2f * cam.orthographicSize / 1080f;
            float d = Vector3.Dot(at - cam.transform.position, cam.transform.forward); return 2f * Mathf.Max(0.5f, d) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / 1080f;
        }

        /// <summary>One line from a to b (chest heights): ours turquoise, dashed 6/4, still; theirs the side's signal colour, dotted 2/5,
        /// marching toward the target, 8 px arrowhead. Offset 4 px to its own right so ours and theirs on one path never overlap.</summary>
        void DrawLine(ref int n, Vector3 a, Vector3 b, bool ours, Color side)
        {
            var sl = Line(n++); float mpp = Mpp((a + b) * 0.5f);
            var dir = b - a; dir.y = 0f; if (dir.sqrMagnitude < 0.01f) { sl.Body.enabled = sl.Head.enabled = false; return; }
            dir.Normalize(); var right = Vector3.Cross(Vector3.up, dir) * (4f * mpp);
            a += right; b += right;
            float w = 2f * mpp, head = 8f * mpp;
            var body = sl.Body; body.widthMultiplier = w; body.sharedMaterial = ours ? oursMat : side == RivalViolet ? rivalMat : policeMat;
            var end = ours ? b : b - dir * head;
            body.SetPosition(0, a); body.SetPosition(1, end); body.enabled = true;
                        // Tile mode repeats the texture per world metre: one texel = one 1080p pixel (dash 6/4 = 10 px, dot 2/5 = 7 px)
            float tiles = 1f / ((ours ? 10f : 7f) * mpp); body.sharedMaterial.SetTextureScale("_BaseMap", new Vector2(tiles, 1f));
            if (!ours) body.sharedMaterial.SetTextureOffset("_BaseMap", new Vector2(-Time.unscaledTime * 1.2f, 0f)); // marching toward the target
            var hd = sl.Head;
            if (ours || !Fights.Hud.arrows) { hd.enabled = false; if (!ours) { body.SetPosition(1, b); } return; }
            hd.sharedMaterial = side == RivalViolet ? headRival : headPolice; hd.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)); hd.widthMultiplier = head;
            hd.SetPosition(0, end); hd.SetPosition(1, b); hd.enabled = true;
        }

        /// <summary>Each frame (freeze and target mode only): our queued lines (to the target, through the walk's stop point with a ghost of the
        /// agent standing there), badges for the HUD, and every hostile's current focus (engine truth, Gun.Target) marching in its colour.</summary>
        void FightLabFrame()
        {
            LineBadges.Clear(); KiteBadges.Clear();
            if (viewRoot == null) { sigLines.Clear(); ghosts.Clear(); return; }
            sigLines.RemoveAll(l => !l.Body); ghosts.RemoveAll(g => !g.body);
            SignalCamera();
            int n = 0, gh = 0;
            if (Frozen && Game != null && unlitTemplate)
            {
                if (!dashTex) dashTex = Pattern(6, 4); if (!dotTex) dotTex = Pattern(2, 5);
                if (!oursMat) oursMat = SigMat(CellTurquoise, dashTex); if (!rivalMat) rivalMat = SigMat(RivalViolet, dotTex); if (!policeMat) policeMat = SigMat(PoliceAmber, dotTex);
                if (!headRival) headRival = SigMat(RivalViolet, null); if (!headPolice) headPolice = SigMat(PoliceAmber, null);
                if (!ghostMat2) ghostMat2 = GhostMat(CellTurquoise);
                var chest = Vector3.up * 1.2f;
                for (int k = 0; k < 4; k++)
                {
                    var fromW = DrawnAt(Op.Name((OpId)k)); if (!fromW.HasValue) continue;
                    var from = fromW.Value + chest; var q = Queued[k];
                    string tgt = VolleyShots[k] > 0 ? VolleyTarget[k] : q != null && q.Do is "fire" or "volley" ? q.Target : null;
                    if (tgt != null && DrawnAt(tgt) is Vector3 tw)
                    {
                        Pt? stop = null;
                        if (VolleyShots[k] > 0) { VolleyPreview v = null; Guard("volley", () => v = Game.PreviewVolley((OpId)k, tgt, VolleyShots[k])); if (v != null && v.Walk) stop = v.StopAt; }
                        else if (Game.Lab) { FirePreview p = null; Guard("fire", () => p = Game.PreviewFire((OpId)k, tgt, q.Force)); if (p != null && p.Walk) stop = p.StopAt; }
                        var to = tw + chest;
                        if (stop.HasValue)
                        {
                            var sw = frame.World(stop.Value); DrawLine(ref n, from, sw + chest, true, CellTurquoise); DrawLine(ref n, sw + chest, to, true, CellTurquoise);
                            Ghost(gh++, sw, Op.Name((OpId)k), tw);
                        }
                        else DrawLine(ref n, from, to, true, CellTurquoise);
                        LineBadges.Add(($"F{k + 1}{(VolleyShots[k] > 0 ? $" ×{VolleyShots[k]}" : "")}", to));
                        KiteAnswerFor(ref n, ref gh, k, tgt, stop);
                        continue;
                    }
                    if (q == null) continue;
                    Vector3? dest = q.Point.HasValue ? frame.World(q.Point.Value) : q.Target.Length > 0 ? WorldOf(q.Target) : q.To.Length > 0 && Game.S.HasPlace(q.To) ? frame.World(Game.S.At(q.To)) : null;
                    if (dest.HasValue) { DrawLine(ref n, from, dest.Value + Vector3.up * 0.1f, true, CellTurquoise); LineBadges.Add(($"F{k + 1}", dest.Value + Vector3.up * 0.1f)); }
                }
                void Focus(string id, Gun gun)
                {
                    if (gun == null || gun.Down || string.IsNullOrEmpty(gun.Target)) return;
                    var a = DrawnAt(id); var b = DrawnAt(gun.Target); if (!a.HasValue || !b.HasValue) return;
                    DrawLine(ref n, a.Value + chest, b.Value + chest, false, SideColour(id));
                }
                foreach (var h in Game.Hostiles) if (h.Present) Focus(h.Id, h.Gun);
                foreach (var id in new[] { "guard", "patrol", "rivals" }) Focus(id, Game.GunOf(id));
                foreach (var sg in Game.SecGuns) Focus(sg.Id, sg);
            }
            else if (Game != null && Game.Lab && unlitTemplate && LabFire is { Walk: true } hp && LabFireAgent >= 0 && DrawnAt(Op.Name((OpId)LabFireAgent)) is Vector3 aw && DrawnAt(LabFireTarget) is Vector3 tw2)
            {   // Keel's walk-in on the hovered target (real time): the route to the stop point, a ghost standing there, the line on to the target
                if (!dashTex) dashTex = Pattern(6, 4); if (!oursMat) oursMat = SigMat(CellTurquoise, dashTex);
                if (!ghostMat2) ghostMat2 = GhostMat(CellTurquoise);
                var chest = Vector3.up * 1.2f; var sw = frame.World(hp.StopAt);
                DrawLine(ref n, aw + chest, sw + chest, true, CellTurquoise); DrawLine(ref n, sw + chest, tw2 + chest, true, CellTurquoise); Ghost(gh++, sw, Op.Name((OpId)LabFireAgent), tw2);
            }
            if (!Frozen && Game != null && Game.Lab && unlitTemplate && LabFire != null && LabFireAgent >= 0 && LabFireTarget.Length > 0)
            {   // the kite's answer on the hovered target, walk-in or not
                if (!dashTex) dashTex = Pattern(6, 4); if (!dotTex) dotTex = Pattern(2, 5); if (!oursMat) oursMat = SigMat(CellTurquoise, dashTex); if (!ghostMat2) ghostMat2 = GhostMat(CellTurquoise);
                KiteAnswerFor(ref n, ref gh, LabFireAgent, LabFireTarget, LabFire.Walk ? LabFire.StopAt : (Pt?)null);
            }
            FreezeGrade(Frozen && !Fights.Hud.rimOnly);
            for (int i = n; i < sigLines.Count; i++) { sigLines[i].Body.enabled = false; sigLines[i].Head.enabled = false; }
            for (int i = gh; i < ghosts.Count; i++) { ghosts[i].body.SetActive(false); ghosts[i].ring.enabled = false; }
        }

        /// <summary>Glyph §3 #4's alternative: in a freeze the world desaturates (the scene volumes' Color Adjustments; the signal layer's camera
        /// has no post, so the signals never shift). Restored on release. Presentation only.</summary>
        void FreezeGrade(bool on)
        {
            if (on == gradeOn) return; gradeOn = on;
            if (on) { gradeSaved.Clear(); foreach (var v in FindObjectsByType<UnityEngine.Rendering.Volume>()) if (v.profile != null && v.profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var ca)) { gradeSaved.Add((ca, ca.saturation.overrideState, ca.saturation.value)); ca.saturation.overrideState = true; ca.saturation.value = Mathf.Max(-100f, ca.saturation.value - 45f); } }
            else { foreach (var (ca, st, val) in gradeSaved) if (ca != null) { ca.saturation.value = val; ca.saturation.overrideState = st; } gradeSaved.Clear(); }
        }
        bool gradeOn; readonly List<(UnityEngine.Rendering.Universal.ColorAdjustments ca, bool state, float value)> gradeSaved = new();

        /// <summary>A ghost of the agent standing at a walk's stop point (Keel's StopAt): the agent's own figure (its LOD0 baked in the pose it
        /// has when the ghost first shows), pale turquoise and unlit on the signal layer, facing the target, over a dashed ring. A capsule when
        /// the agent has no skinned figure.</summary>
        void Ghost(int i, Vector3 at, string viewId, Vector3 faceTo, Material mat = null)
        {
            while (ghosts.Count <= i)
            {
                var body = new GameObject("signal-ghost") { layer = SignalLayer }; body.transform.SetParent(viewRoot, false);
                body.AddComponent<MeshFilter>(); var mr = body.AddComponent<MeshRenderer>(); mr.sharedMaterial = ghostMat2; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                var rg = new GameObject("signal-ghost-ring") { layer = SignalLayer }; rg.transform.SetParent(viewRoot, false);
                var lr = rg.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 24; lr.alignment = LineAlignment.View; lr.textureMode = LineTextureMode.Tile;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.sharedMaterial = oursMat;
                ghosts.Add((body, lr));
            }
            var (b, r) = ghosts[i]; b.SetActive(true);
            var (mesh, rel) = GhostMesh(viewId); mat = mat ? mat : ghostMat2;
            b.GetComponent<MeshFilter>().sharedMesh = mesh;
            var gmr = b.GetComponent<MeshRenderer>(); if (gmr.sharedMaterials.Length != mesh.subMeshCount || gmr.sharedMaterial != mat) { var ms = new Material[Mathf.Max(1, mesh.subMeshCount)]; for (int j = 0; j < ms.Length; j++) ms[j] = mat; gmr.sharedMaterials = ms; } // every submesh (a figure's body is not always the first)
            var face = faceTo - at; face.y = 0f; var rot = face.sqrMagnitude > 0.01f ? Quaternion.LookRotation(face.normalized, Vector3.up) : Quaternion.identity;
            var m = Matrix4x4.TRS(at, rot, Vector3.one) * rel;
            b.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation); b.transform.localScale = m.lossyScale;
            float rad = 0.45f; r.widthMultiplier = 2f * Mpp(at); for (int j = 0; j < 24; j++) { float t = j / 24f * Mathf.PI * 2f; r.SetPosition(j, at + new Vector3(Mathf.Cos(t) * rad, 0.05f, Mathf.Sin(t) * rad)); }
            r.sharedMaterial = mat == ghostRival && rivalMat ? rivalMat : mat == ghostPolice && policeMat ? policeMat : oursMat; // the ring in the ghost's own side colour
            r.enabled = true;
        }

        /// <summary>A ghost's material: unlit, the side's colour at 55 % (Glyph: a ghost must not read as one more member of the cell), with depth
        /// write so the figure's own overlapping parts never stack into a solid body.</summary>
        Material GhostMat(Color c)
        {
            var m = new Material(fanTemplate ? fanTemplate : unlitTemplate) { name = "signal-ghost" }; m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 0.55f));
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 1f); return m;
        }
        [NonSerialized] Material ghostRival, ghostPolice;

        /// <summary>Brief 027 F2 (Pulse's kite, Keel's PreviewAnswer): what a hostile does if this agent fires on it from where it will stand.
        /// Police walk to Point and are there in Ticks; rivals go to cover (or a blind point). Drawn as that hostile's ghost at Point in its
        /// side colour, its dotted marching line there, and (KiteBadges, for the HUD) a clock glyph and the seconds. Read only.</summary>
        public readonly List<(string side, Vector3 at, int s)> KiteBadges = new();
        void KiteAnswerFor(ref int n, ref int gh, int k, string target, Pt? from)
        {
            if (Game == null || !Game.Lab || string.IsNullOrEmpty(target)) return;
            KiteAnswer a = null; Guard("kite", () => a = Game.PreviewAnswer((OpId)k, target, from)); if (a == null) return;
            var hw = DrawnAt(target); if (!hw.HasValue) return;
            var pw = frame.World(a.Point); bool rival = a.Side == "rivals";
            if (!ghostRival) ghostRival = GhostMat(RivalViolet); if (!ghostPolice) ghostPolice = GhostMat(PoliceAmber);
            if (!rivalMat) rivalMat = SigMat(RivalViolet, dotTex); if (!policeMat) policeMat = SigMat(PoliceAmber, dotTex);
            if (!headRival) headRival = SigMat(RivalViolet, null); if (!headPolice) headPolice = SigMat(PoliceAmber, null);
            var chest = Vector3.up * 1.2f; DrawLine(ref n, hw.Value + chest, pw + chest, false, rival ? RivalViolet : PoliceAmber);
            var face = from.HasValue ? frame.World(from.Value) : DrawnAt(Op.Name((OpId)k)) ?? pw;
            Ghost(gh++, pw, target, face, rival ? ghostRival : ghostPolice);
            KiteBadges.Add((a.Side, pw, (a.Ticks + Game.Hz - 1) / Game.Hz));
        }

        readonly Dictionary<string, (Mesh mesh, Matrix4x4 rel)> ghostMeshes = new(); [NonSerialized] Mesh ghostCapsule;
        /// <summary>The agent's figure baked once (scale included), with its pose relative to the agent's root (root yaw removed); else a capsule.</summary>
        (Mesh, Matrix4x4) GhostMesh(string viewId)
        {
            if (ghostMeshes.TryGetValue(viewId, out var gm) && gm.mesh) return gm;
            var root = views.Find(ViewKey(viewId));
            SkinnedMeshRenderer smr = null;
            if (root) foreach (var s in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { if (smr == null || s.name.EndsWith("_LOD0")) smr = s; if (s.name.EndsWith("_LOD0")) break; }
            if (smr && smr.sharedMesh)
            {
                var mesh = new Mesh { name = "ghost-" + viewId }; smr.BakeMesh(mesh, true);
                var rootM = Matrix4x4.TRS(root.position, root.rotation, Vector3.one);
                var rel = rootM.inverse * Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                return ghostMeshes[viewId] = (mesh, rel);
            }
            if (!ghostCapsule) { var c = GameObject.CreatePrimitive(PrimitiveType.Capsule); ghostCapsule = c.GetComponent<MeshFilter>().sharedMesh; Destroy(c); }
            return (ghostCapsule, Matrix4x4.TRS(Vector3.up * 0.875f, Quaternion.identity, new Vector3(0.5f, 0.875f, 0.5f)));
        }

        // ---------------------------------------------------------------- the outcome line (playground.md §5)

        /// <summary>One line per try, for Sky's notes and Suture's table: out/play/fightlab-outcomes.tsv.</summary>
        void FightLabOutcome()
        {
            try
            {
                var g = Game; int hp = 0, max = 0, cellDown = 0, hostDown = 0;
                foreach (var op in g.Ops) { if (op.Gun.W == null) continue; hp += op.Gun.Health; max += op.Gun.Max; if (op.Down) cellDown++; }
                foreach (var h in g.Hostiles) if (h.Down) hostDown++;
                foreach (var s in g.SecGuns) if (s.Down) hostDown++;
                if (g.GuardDown) hostDown++; if (g.PatrolDown) hostDown++;
                var dir = Path.GetDirectoryName(SessionPath); var f = Path.Combine(dir, "fightlab-outcomes.tsv");
                if (!File.Exists(f)) File.WriteAllText(f, "when\tpreset\tmode\tcheckpoint\tresult\tcell health\tcell down\thostiles down\tTrail\tAlert\tStanding\tfreezes\tvolleys\twall s\tsession\n");
                File.AppendAllText(f, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{FightRunPreset}\t{Fights.fightMode}\t{(FightRunCheckpoint.Length > 0 ? FightRunCheckpoint : "-")}\t{g.Result}{(g.FailCode.Length > 0 ? $" ({g.FailCode})" : "")}\t{hp}/{max}\t{cellDown}\t{hostDown}\t{g.Trail}\t{g.Alert}\t{g.Standing}\t{FreezeCount}\t{VolleyCount}\t{Time.unscaledTime - wallStart:0}\t{Path.GetFileName(SessionPath)}\n");
            }
            catch (Exception e) { Debug.LogWarning($"FightLab: outcome not written: {e.Message}"); }
        }
    }
}
