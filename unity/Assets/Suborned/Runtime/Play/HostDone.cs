using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 043, the done moment (Glyph's design, port table §6; Fable's words text.json less.done; Keel's engine 0.24.0 Windows()).
    /// When an act lands the player sees that it finished (a tick on the agent in the outcome's shape, the portrait plate's word for 2.2 s) and
    /// what it gave: the gain chip over the thing that changed (the doer's face, one icon-and-word line per fact, at most three, and the unlock
    /// row: the verbs that just opened, a refused way struck), shown without hover for 4.2 s. A newly opened act is fresh for 90 s: it leads the
    /// hold's offer (MainWay) and carries a lit pip on the pill and the ring. An open window (the guard away, a camera blind or looped, the patrol
    /// rerouted, the rivals misled) carries a marker with its seconds or the Tech's face; when it closes a pop says so on the thing. Every landing
    /// and every cancel leaves a row in the notes (L). A new order that would drop an act says so before you let go (CancelTag) and when it
    /// happens (ActCancelled: a pop on the agent, the plate word, a row). The log scan also ends a lasting fire order out of range (engine 0.21:
    /// fire.out-of-range, "Too far"), pops a walk-in dropped at its start (engine 0.23: order.dropped) and a walk-in ended by another order.
    /// One function (LandingOf) reads every landing, so the tick, the plate, the chip and the row cannot disagree. View only: the engine sees
    /// the same orders, so replays are untouched.</summary>
    public sealed partial class HandoverHost
    {
        public const float DoneChipS = 4.2f, DonePlateS = 2.2f, DoneTickS = 1.2f, DonePopS = 1.8f, FreshS = 90f;

        public sealed class GainLine { public string Icon = "", Word = ""; }
        public sealed class UnlockPill { public string Icon = "", Word = ""; public bool Shut; }
        /// <summary>One landing's chip: who, what kind of act (its colour), the tone (ok filled, soft not yet, bad refused or reported), where.</summary>
        public sealed class DoneChip
        {
            public int Agent, Id; public string Do = "", Approach = "", Tone = "ok", AnchorKey = ""; public Vector3? At; public float Born;
            public readonly List<GainLine> Lines = new(); public readonly List<UnlockPill> Unlocks = new(); public string Role = "";
        }
        public readonly List<DoneChip> DoneChips = new();
        /// <summary>An open window's marker: what it is on (a view key), its seconds left (-1 held), the face that holds it (-1 none).</summary>
        public sealed class WindowMark { public string Kind = "", Thing = "", Key = ""; public int Until = -1, Face = -1, Left = -1, Total = -1; }
        /// <summary>Each open window's length in ticks, taken when it first shows (or its end moves): the marker's draining underline.</summary>
        readonly Dictionary<string, (int until, int span)> windowSpan = new();
        public readonly List<WindowMark> WindowMarks = new();

        readonly float[] doneUntil = new float[4], tickAt = { -9f, -9f, -9f, -9f }, cancelPopAt = { -9f, -9f, -9f, -9f };
        readonly string[] doneWord = { "", "", "", "" }, doneTone = { "", "", "", "" };
        readonly Order[] actSeen = new Order[4];
        readonly Dictionary<string, float> fresh = new(); readonly Dictionary<string, int> freshSeq = new(); int landSeq;
        HashSet<string> openBefore = new(), shutBefore = new();
        readonly Dictionary<string, (bool sight, bool routine)> knownBefore = new();
        readonly Dictionary<string, int> windowsBefore = new();
        Suborned.Sim.Game doneGame; int doneLog, chipIds;

        // the proof's counters and last words
        public int DoneLandings { get; private set; }
        public int DoneCancelPops { get; private set; }
        public int WindowClosePops { get; private set; }
        public int OutOfRangePops { get; private set; }
        public string LastCancelPop { get; private set; } = "";
        public string LastClosePop { get; private set; } = "";
        public DoneChip LastChip { get; private set; }

        /// <summary>The portrait plate's word after a landing or a cancel ("done", "turned", "not yet", "refused", "reported", "cancelled"), "" none.</summary>
        public string DonePlate(int k) => k >= 0 && k < 4 && Time.unscaledTime < doneUntil[k] ? doneWord[k] : "";
        /// <summary>The landing's tone for agent k ("ok", "soft", "bad", "cancel"), for the plate's icon and the agent's tick.</summary>
        public string DoneTone(int k) => k >= 0 && k < 4 ? doneTone[k] : "";
        /// <summary>The agent's tick: unscaled seconds since the landing (large when none).</summary>
        public float DoneTickAge(int k) => k >= 0 && k < 4 ? Time.unscaledTime - tickAt[k] : 99f;

        string DW(string key, string fallback) => Text?.DoneWord(key, fallback) ?? fallback;

        void DoneReset() { DoneChips.Clear(); WindowMarks.Clear(); fresh.Clear(); freshSeq.Clear(); openBefore.Clear(); shutBefore.Clear(); knownBefore.Clear(); windowsBefore.Clear(); windowSpan.Clear(); for (int k = 0; k < 4; k++) { doneUntil[k] = 0f; tickAt[k] = -9f; cancelPopAt[k] = -9f; doneWord[k] = ""; doneTone[k] = ""; actSeen[k] = null; } }

        // ---------------------------------------------------------------- fresh: a newly opened act leads for 90 s

        static string VerbKey(Order o) => o == null ? "" : o.Do == "turn" ? o.Approach : o.Do;
        static string FreshKey(string target, string verb) => target + "|" + verb;
        string FreshTarget(Target t) => t == null ? "" : t.Kind == "person" ? t.Key : t.Kind == "door" || t.Kind == "camera" && t.Key == "entrance" ? "door" : t.Kind == "car" ? "car" : t.Kind;
        /// <summary>The act o on t opened within the last 90 s and has not been given since (the pill's and the ring's lit pip; MainWay's lead).</summary>
        public bool Fresh(Target t, Order o) => o != null && t != null && fresh.TryGetValue(FreshKey(o.Do is "enter" ? "door" : o.Do == "board" ? "car" : FreshTarget(t), VerbKey(o)), out var until) && Time.unscaledTime < until;
        /// <summary>Which landing opened it (higher is newer): among fresh acts the newest leads; -1 not fresh.</summary>
        public int FreshRank(Target t, Order o) => Fresh(t, o) && freshSeq.TryGetValue(FreshKey(o.Do is "enter" ? "door" : o.Do == "board" ? "car" : FreshTarget(t), VerbKey(o)), out var s) ? s : -1;
        void Unfresh(Target t, Order o) { if (o == null || t == null) return; fresh.Remove(FreshKey(o.Do is "enter" ? "door" : o.Do == "board" ? "car" : FreshTarget(t), VerbKey(o))); }

        // ---------------------------------------------------------------- the gates (the unlock row is a diff of these, never a list by hand)

        static readonly string[] Roles3 = { "waitress", "driver", "guard" };
        static readonly string[] Approaches = { "ask", "pay", "help", "pressure" };

        /// <summary>The acts open now ("role|verb") and the ways shut by a remembered refusal, read from the engine's own state and queries.</summary>
        (HashSet<string> open, HashSet<string> shut) Gates()
        {
            var open = new HashSet<string>(); var shut = new HashSet<string>(); var g = Game; if (g == null || g.Range) return (open, shut);
            foreach (var role in Roles3)
            {
                Person p = null; try { p = g.PersonByRole(role); } catch (Exception) { } if (p == null) continue;
                Answer a = default; bool got = Guard("gate listen", () => a = g.LearnQuery("scout", "listen", role));
                if (got && (a.ok || a.code is "too-far" or "agent-busy" or "no-clear-line" or "no-walk" or "run-tethered")) open.Add(role + "|listen");
                if (!p.Turned)
                    foreach (var ap in Approaches)
                    {
                        if (p.KnownRefused.Contains(ap)) { shut.Add(role + "|" + ap); continue; }
                        var need = g.R.ApproachRequires(ap); if (need == "nothing" || p.Knows(need)) open.Add(role + "|" + ap);
                    }
                else if (role is "waitress" or "driver") open.Add(role + "|call");
            }
            bool guardAway = false, loop = false; foreach (var w in g.Windows()) { if (w.Kind == "call" && w.Thing == "guard") guardAway = true; if (w.Kind == "loop") loop = true; }
            if (g.Vale == ValeAt.Office && guardAway && loop) open.Add("door|enter");
            if (g.Vale == ValeAt.Pickup) open.Add("councillor|take");
            if (g.Vale == ValeAt.WithEnforcer) open.Add("car|board");
            return (open, shut);
        }

        // ---------------------------------------------------------------- the landing detector (one function for the tick, plate, chip, row)

        public struct Landing { public int Agent; public string Code, Outcome, Role, Do, Approach; }

        /// <summary>Whose act a log line lands, with its outcome (done, turned, kept, not-yet, refused, suspicious), or Agent -1.</summary>
        public static Landing LandingOf(Suborned.Sim.Game g, LogLine l)
        {
            var c = l.Code ?? ""; var no = new Landing { Agent = -1 };
            string role = RoleIn(l);
            Landing L(int k, string d, string outcome = "done", string ap = "") => new Landing { Agent = k, Code = c, Outcome = outcome, Role = role ?? "", Do = d, Approach = ap };
            if (c.EndsWith(".watch.watched", StringComparison.Ordinal)) { var who = c[..c.IndexOf('.')]; return IsOpName(who) ? L((int)Op.Parse(who), "watch") : no; }
            if (c.StartsWith("scout.listen.", StringComparison.Ordinal)) return L((int)OpId.Scout, "listen");
            if (c == "rivals.misled") return L((int)OpId.Scout, "mislead");
            if (c.StartsWith("turn.", StringComparison.Ordinal))
            {
                var parts = c.Split('.'); if (parts.Length < 3) return no; string outcome = parts[^1];
                if (outcome is not ("turned" or "kept" or "not-yet" or "refused" or "suspicious")) return no;
                foreach (var x in g.Log) if (x.Tick == l.Tick && x.Code == "report") { outcome = "suspicious"; break; } // reported: the suspicious voice (SayLanded)
                return L((int)OpId.Fixer, "turn", outcome, parts[1]);
            }
            if (c.StartsWith("call.", StringComparison.Ordinal) && c[5..] is "waitress" or "driver" or "guard") return L((int)OpId.Fixer, "call");
            if (c == "loop.on") return L((int)OpId.Tech, "loop");
            if (c == "patrol.rerouted") return L((int)OpId.Tech, "reroute");
            if (c is "take.done" or "enter.in" or "enter.out") return L((int)OpId.Enforcer, c[..c.IndexOf('.')]);
            if (c is "takedown.out" or "takedown.dead") return L(AgentIn(l.Text, (int)OpId.Enforcer), "takedown");
            if (c == "board.in") return L(AgentIn(l.Text, (int)OpId.Enforcer), "board");
            if (c == "carry.body") return L(AgentIn(l.Text, (int)OpId.Enforcer), "carry");
            if (c.StartsWith("stash.", StringComparison.Ordinal) && c is not "stash.found") return L(AgentIn(l.Text, (int)OpId.Enforcer), "stash");
            if (c == "restrain.done") return L(AgentIn(l.Text, (int)OpId.Enforcer), "restrain");
            if (c == "carry.down") return L(AgentIn(l.Text, (int)OpId.Enforcer), "drop");
            return no;
        }

        static int AgentIn(string text, int fallback)
        {
            var t = (text ?? "").ToLowerInvariant(); int best = -1, at = int.MaxValue;
            for (int k = 0; k < 4; k++) { int i = t.IndexOf(Op.Name((OpId)k), StringComparison.Ordinal); if (i >= 0 && i < at) { at = i; best = k; } }
            return best >= 0 ? best : fallback;
        }

        // ---------------------------------------------------------------- per frame

        /// <summary>Once a frame after the step: the log since the last frame (landings, fire out of range, walk-ins dropped or ended), the
        /// windows (markers, and a pop on each that closed), and the gates (kept for the next landing's unlock row).</summary>
        int gateTick = -1, gateLog = -1, gateOrders = -1;
        void DoneFrame()
        {
            var g = Game; if (g == null) return; float now = Time.unscaledTime;
            if (!ReferenceEquals(g, doneGame))
            {
                doneGame = g; doneLog = g.Log.Count; gateTick = gateLog = gateOrders = -1; DoneReset(); (openBefore, shutBefore) = Gates(); WindowsRead(false); KnownSnap();
                return;
            }
            var made = new List<DoneChip>();
            for (int i = doneLog; i < g.Log.Count; i++)
            {
                var l = g.Log[i]; var c = l.Code ?? "";
                if (c == "fire.out-of-range") { int k = AgentIn(l.Text.Split(':')[0], -1); if (k >= 0) { LocksDrop(new[] { k }); PopKey("fire-out-of-range", null, AgentWorld(k), Feedback.Info); OutOfRangePops++; } continue; }
                if (c == "order.dropped") { DroppedPop(l); continue; }
                if (c == "order.walk-in-ends") { int k = AgentIn(l.Text.Split(':')[0], -1); if (k >= 0 && now - cancelPopAt[k] > 0.5f && actSeen[k] != null) ActCancelPop(k, actSeen[k], ""); continue; }
                if (Ending.Length > 0) continue;
                var land = LandingOf(g, l); if (land.Agent < 0) continue;
                var chip = Landed(land, l); if (chip == null) continue;
                if (g.Tick - l.Tick > 2 * Game.Hz) { DoneChips.Remove(chip); doneUntil[land.Agent] = 0f; tickAt[land.Agent] = -9f; continue; } // a jump (a replay, a shot's fast-forward): the notes keep the row, no chip
                made.Add(chip);
            }
            doneLog = g.Log.Count;
            // brief 045 F4 (Redline: three listen gate queries a frame, each up to 120 route searches, 0.5–1.3 ms): the gates, the watchers'
            // knowledge and the windows change only with the engine: read them once per tick, log line or order, not every frame
            if (made.Count == 0 && g.Tick == gateTick && g.Log.Count == gateLog && g.Orders.Count == gateOrders)
            {
                for (int k = 0; k < 4; k++) actSeen[k] = g.Ops[k].Busy ?? g.Ops[k].Act ?? pending[k];
                DoneChips.RemoveAll(x => now - x.Born > DoneChipS); return;
            }
            gateTick = g.Tick; gateLog = g.Log.Count; gateOrders = g.Orders.Count;
            // the unlock row: the gates now against the last frame's
            var (open, shut) = Gates();
            if (made.Count > 0) landSeq++;
            foreach (var key in open) if (!openBefore.Contains(key)) { fresh[key] = now + FreshS; freshSeq[key] = landSeq; } // the newest news leads (gains.js freshSeq)
            foreach (var key in shut) fresh.Remove(key);
            if (made.Count > 0)
            {
                foreach (var key in open)
                {
                    if (openBefore.Contains(key)) continue;
                    var parts = key.Split('|'); var chip = made.Find(x => x.Role == parts[0]) ?? made[^1];
                    if (chip.Unlocks.Count < 3 && !chip.Unlocks.Exists(u => u.Word == VerbWord(parts[1]))) chip.Unlocks.Add(new UnlockPill { Icon = HudModel.AtlasIcon(parts[1] is "ask" or "pay" or "help" or "pressure" ? "turn" : parts[1], parts[1], ""), Word = VerbWord(parts[1]) });
                }
                foreach (var key in shut)
                {
                    if (shutBefore.Contains(key)) continue; var parts = key.Split('|'); var chip = made.Find(x => x.Role == parts[0]) ?? made[^1];
                    if (chip.Unlocks.Count < 3) chip.Unlocks.Add(new UnlockPill { Icon = HudModel.AtlasIcon("turn", parts[1], ""), Word = VerbWord(parts[1]), Shut = true });
                }
            }
            openBefore = open; shutBefore = shut;
            KnownSnap();
            WindowsRead(true);
            for (int k = 0; k < 4; k++) actSeen[k] = g.Ops[k].Busy ?? g.Ops[k].Act ?? pending[k];
            DoneChips.RemoveAll(x => now - x.Born > DoneChipS);
        }

        Vector3? AgentWorld(int k) { var tr = views?.Find("op/" + Op.Name((OpId)k)); return tr && tr.gameObject.activeInHierarchy ? tr.position : Game != null ? frame.World(Game.Ops[k].Pos) : (Vector3?)null; }

        /// <summary>The ring label of a verb key (text.json actions.ring-*): Listen, Help, Call, Enter, Board.</summary>
        string VerbWord(string key)
        {
            if (Text != null && Text.Actions.TryGetValue("ring-" + key, out var a) && a.Label.Length > 0) return a.Label;
            return key == "takedown" ? "Take down" : key.Length > 0 ? char.ToUpperInvariant(key[0]) + key[1..] : key;
        }
        string VerbWord(Order o) => o == null ? "" : o.Do == "takedown" ? VerbWord("takedown") : VerbWord(VerbKey(o));

        /// <summary>The watchers' knowledge before a landing (a camera's or the patrol's cone, its routine): the chip's "Cone" and "Rounds".</summary>
        void KnownSnap()
        {
            knownBefore.Clear(); if (Game == null || Game.Range) return;
            foreach (var w in Game.WatcherIds()) { var kn = Game.Known(w); knownBefore[w] = kn == null ? (false, false) : (kn.Sight, kn.Routine); }
            foreach (var role in Roles3) { Person p = null; try { p = Game.PersonByRole(role); } catch (Exception) { } if (p != null) knownBefore["p:" + role] = (p.KRoutine, p.KNeed); }
        }

        /// <summary>A landing: the plate word, the tick, the chip (lines from what changed), the history row.</summary>
        DoneChip Landed(Landing land, LogLine l)
        {
            var g = Game; int k = land.Agent; float now = Time.unscaledTime; DoneLandings++;
            var chip = new DoneChip { Agent = k, Id = ++chipIds, Do = land.Do, Approach = land.Approach, Born = now, Role = land.Role };
            void Line(string icon, string word) { if (chip.Lines.Count < 3 && word.Length > 0) chip.Lines.Add(new GainLine { Icon = icon, Word = word }); }
            string plate = "done", tone = "ok"; Person p = null; if (land.Role.Length > 0) try { p = g.PersonByRole(land.Role); } catch (Exception) { }
            switch (land.Do)
            {
                case "watch":
                    if (p != null)
                    {
                        chip.AnchorKey = land.Role; bool hadRoutine = knownBefore.TryGetValue("p:" + land.Role, out var kb) && kb.sight;
                        if (p.KRoutine && !hadRoutine) Line("routine", DW("gain.rounds", "Rounds"));
                        if (land.Role == "guard" && Game.Known("guard") is { Sight: true } gk && !(knownBefore.TryGetValue("guard", out var gb) && gb.sight)) Line("cone", DW("gain.cone", "Cone"));
                        if (chip.Lines.Count == 0) Line("dot", DW("gain.nothing-new", "Nothing new"));
                    }
                    else
                    {
                        var mm = System.Text.RegularExpressions.Regex.Match(l.Text ?? "", @"watched the (\w+)( camera)?");
                        string w = !mm.Success ? "" : mm.Groups[2].Success ? "camera:" + mm.Groups[1].Value : mm.Groups[1].Value;
                        chip.AnchorKey = w.StartsWith("camera:") ? "camera/" + w[7..] : w;
                        var kn = w.Length > 0 ? Game.Known(w) : null; knownBefore.TryGetValue(w, out var wb);
                        if (kn != null && kn.Sight && !wb.sight) Line("cone", DW("gain.cone", "Cone"));
                        if (kn != null && kn.Routine && !wb.routine) Line("routine", DW("gain.rounds", "Rounds"));
                        if (chip.Lines.Count == 0) Line("dot", DW("gain.nothing-new", "Nothing new"));
                    }
                    break;
                case "listen":
                    chip.AnchorKey = land.Role;
                    if (p != null)
                    {
                        if (p.KNeed) { string lb = ""; try { lb = g.NeedRecordOf(p).label ?? ""; } catch (Exception) { } Line(p.Need.Length > 0 ? p.Need : "unknown", lb.Length > 0 ? lb : Intel("need", p.Need)); }
                        if (p.KTemperament) Line(p.Temperament.Length > 0 ? p.Temperament : "unknown", Intel("temperament", p.Temperament));
                        if (p.KSecret && p.Secret.Length > 0) { string lover = p.Lover != null ? Text?.TargetName(p.Lover.Role, p.Lover.Name) ?? p.Lover.Name : ""; Line(p.Secret == "affair" ? "kin" : "secret", Intel("secret", p.Secret, lover)); }
                        if (chip.Lines.Count == 0) Line("dot", DW("gain.nothing-new", "Nothing new"));
                    }
                    break;
                case "turn":
                    chip.AnchorKey = land.Role; plate = DW("plate." + land.Outcome, land.Outcome == "not-yet" ? "not yet" : land.Outcome == "suspicious" ? "reported" : land.Outcome == "kept" ? "turned" : land.Outcome);
                    switch (land.Outcome)
                    {
                        case "turned": case "kept": Line("turned", DW("gain.contact", "Contact")); break;
                        case "not-yet": Line("asked", DW("gain.not-yet", "Not yet")); tone = "soft"; break;
                        case "refused": Line("refused", DW("gain.refused", "Refused")); tone = "bad"; break;
                        default: Line("p_alert", DW("gain.reported", "Reported")); tone = "bad"; break;
                    }
                    break;
                case "call":
                    if (l.Code == "call.driver") { chip.AnchorKey = "camera/pickup"; Line("eye_shut", DW("gain.camera-blind", "Camera blind")); }
                    else if (l.Code == "call.waitress") { chip.AnchorKey = "guard"; Line("eye_shut", DW("gain.guard-away", "Guard away")); }
                    else { chip.AnchorKey = l.Code[5..]; Line("call", VerbWord("call")); }
                    break;
                case "loop": chip.AnchorKey = "camera/" + g.LoopCam; Line("eye_shut", DW("gain.camera-dark", "Camera dark")); break;
                case "reroute": chip.AnchorKey = "patrol"; Line("reroute", DW("gain.patrol-away", "Patrol away")); break;
                case "mislead": chip.AnchorKey = "rivals/0"; Line("mislead", DW("gain.rivals-away", "Rivals away")); break;
                case "enter":
                    chip.AnchorKey = "op/enforcer";
                    if (l.Code == "enter.in") Line("enter", DW("gain.inside", "Inside"));
                    else if (g.Vale == ValeAt.WithEnforcer) Line("take", DW("gain.vale", "Vale")); else { Line("refused", DW("gain.no-vale", "No Vale")); tone = "bad"; }
                    break;
                case "take": chip.AnchorKey = "councillor"; Line("take", DW("gain.vale", "Vale")); break;
                case "board": chip.AnchorKey = "car/getaway"; Line("car", DW("gain.aboard", "Aboard")); break;
                case "takedown":
                    {
                        var who = BodyKeyIn(l.Text); chip.AnchorKey = who.Length > 0 ? "body/" + who : "op/" + Op.Name((OpId)k);
                        if (l.Code == "takedown.out") { Line("body_out", DW("gain.out", "Out")); Line("p_time", DW("gain.wakes", "Wakes {s} s").Replace("{s}", Math.Max(1, g.R.WakeS).ToString())); }
                        else Line("body_dead", DW("gain.dead", "Dead"));
                        break;
                    }
                case "carry": chip.AnchorKey = "op/" + Op.Name((OpId)k); Line("s_carrying", DW("gain.carrying", "Carrying")); break;
                case "stash": chip.AnchorKey = "op/" + Op.Name((OpId)k); Line("stash", DW("gain.hidden", "Hidden")); break;
                case "restrain": chip.AnchorKey = "op/" + Op.Name((OpId)k); Line("restrain", DW("gain.tied", "Tied")); break;
                case "drop": chip.AnchorKey = "op/" + Op.Name((OpId)k); Line("drop", DW("gain.laid-down", "Laid down")); break;
            }
            if (chip.Lines.Count == 0) Line("check", DW("plate.done", "done"));
            if (land.Do == "turn" && land.Outcome == "refused" && land.Role.Length > 0) shutBefore.Remove(land.Role + "|" + land.Approach); // the shut pill is added by the gate diff
            chip.Tone = tone; chip.At = AnchorWorld(chip.AnchorKey) ?? AgentWorld(k);
            doneUntil[k] = now + DonePlateS; doneWord[k] = land.Do == "turn" ? plate : DW("plate.done", "done"); doneTone[k] = tone; tickAt[k] = now;
            DoneChips.Add(chip); while (DoneChips.Count > 4) DoneChips.RemoveAt(0); LastChip = chip;
            DoneRow(k, land.Do == "turn" ? land.Approach : land.Do, chip.AnchorKey, chip.Lines, tone);
            return chip;
        }

        string Intel(string group, string value, string name = "")
        {
            var t = Text?.Intel; string key = group == "secret" ? (value == "affair" && name.Length > 0 ? "affair-named" : value.Length == 0 ? "none" : value) : value;
            if (t != null && t.TryGetValue(group + "." + key, out var w)) return w.Replace("{name}", name);
            return value.Length > 0 ? char.ToUpperInvariant(value[0]) + value[1..] : value;
        }

        string BodyKeyIn(string text)
        {
            if (Game == null) return ""; foreach (var b in Game.Bodies) { var n = PersonName(b.P); if (n.Length > 0 && (text ?? "").Contains(n)) return b.P.Role.Length > 0 ? b.P.Role : b.P.Id; }
            return Game.Bodies.Count > 0 ? (Game.Bodies[^1].P.Role.Length > 0 ? Game.Bodies[^1].P.Role : Game.Bodies[^1].P.Id) : "";
        }

        /// <summary>Where a chip or a marker sits: the view's transform for its key (a role, op/…, camera/…, patrol, rivals/0, car/…, body/…).</summary>
        public Vector3? AnchorWorld(string key)
        {
            if (string.IsNullOrEmpty(key) || views == null) return null; var tr = views.Find(key);
            if ((!tr || !tr.gameObject.activeInHierarchy) && key.StartsWith("body/")) tr = views.Find("body/" + key[5..]);
            return tr && tr.gameObject.activeInHierarchy ? tr.position : (Vector3?)null;
        }

        /// <summary>One row in the notes (L) per landing: "Inez · Bureau loan · Idealist · Loves the guard", the doer's card, unread.</summary>
        void DoneRow(int k, string verb, string anchor, List<GainLine> lines, string tone)
        {
            if (Game == null || rangeMode) return;
            var bits = new List<string>(); string who = IsRole(anchor) ? Cap(Name(anchor)) : ""; if (who.Length > 0) bits.Add(who);
            foreach (var x in lines) bits.Add(x.Word);
            string kind = verb is "watch" or "listen" ? "learned" : verb is "ask" or "pay" or "help" or "pressure" ? (tone == "ok" ? "turned" : "approach") : "act";
            var c = NewCard(kind, string.Join(" · ", bits), Glyph(verb is "ask" or "pay" or "help" or "pressure" ? "turn" : verb, verb), "", AnchorWorld(anchor) ?? AgentWorld(k), anchor);
            c.Agent = k; c.Verb = verb; c.Live = false; c.Ended = Game.Tick; c.Tone = tone == "ok" ? Feedback.Ok : tone == "bad" ? Feedback.Refused : Feedback.Info;
        }

        // ---------------------------------------------------------------- windows

        /// <summary>The open windows (Game.Windows, engine 0.24.0) as markers; a window gone since the last frame pops its close word on the thing.</summary>
        void WindowsRead(bool pops)
        {
            var g = Game; WindowMarks.Clear(); if (g == null || g.Range) { windowsBefore.Clear(); return; }
            List<OpenWindow> ws = null; if (!Guard("windows", () => ws = g.Windows()) || ws == null) return;
            var now = new Dictionary<string, int>();
            foreach (var w in ws)
            {
                var m = new WindowMark { Kind = w.Kind, Thing = w.Thing, Until = w.Until, Key = WindowKey(w.Kind, w.Thing) };
                m.Left = w.Until < 0 ? -1 : Math.Max(0, (w.Until - g.Tick + Game.Hz - 1) / Game.Hz); if (w.Kind == "loop") m.Face = (int)OpId.Tech;
                string sk = w.Kind + "|" + w.Thing; if (w.Until >= 0 && (!windowSpan.TryGetValue(sk, out var sp) || sp.until != w.Until)) windowSpan[sk] = sp = (w.Until, Math.Max(1, w.Until - g.Tick));
                m.Total = w.Until < 0 ? -1 : Math.Max(1, (windowSpan[sk].span + Game.Hz - 1) / Game.Hz);
                WindowMarks.Add(m); now[w.Kind + "|" + w.Thing] = w.Until;
            }
            if (pops && Ending.Length == 0)
                foreach (var kv in windowsBefore)
                {
                    if (now.ContainsKey(kv.Key)) continue; var parts = kv.Key.Split('|');
                    string kind = parts[0] == "call" ? (parts[1] == "guard" ? "guard" : "camera") : parts[0] == "loop" ? "camera" : parts[0] == "reroute" ? "patrol" : "rivals";
                    string word = DW("close." + kind, kind == "guard" ? "Guard back" : kind == "camera" ? "Camera on" : kind == "patrol" ? "Patrol back" : "Rivals back");
                    var at = AnchorWorld(WindowKey(parts[0], parts[1])); if (!at.HasValue) continue;
                    DoneChips.RemoveAll(c => c.AnchorKey == WindowKey(parts[0], parts[1])); // the chip that opened it says no more ("Guard away" with the guard back)
                    Pop(word, null, at.Value, Feedback.Info, DonePopS, "icons/singles/eye_open"); WindowClosePops++; LastClosePop = word;
                }
            windowsBefore.Clear(); foreach (var kv in now) windowsBefore[kv.Key] = kv.Value;
            foreach (var k in new List<string>(windowSpan.Keys)) if (!now.ContainsKey(k)) windowSpan.Remove(k);
        }

        static string WindowKey(string kind, string thing) => kind switch
        {
            "call" => thing == "guard" ? "guard" : "camera/pickup", "loop" => "camera/" + thing, "reroute" => "patrol", "mislead" => "rivals/0", _ => thing,
        };

        // ---------------------------------------------------------------- cancelling

        /// <summary>What a new order to agent k would drop: the act's ring word and its cost ("12 s lost", "They remember"; "" free or not
        /// started), or null when it is on no act (a walk is not an act).</summary>
        public (string verb, string cost)? WouldCancel(int k)
        {
            if (Game == null || k < 0 || k > 3) return null; var op = Game.Ops[k];
            var o = op.Busy ?? op.Act ?? pending[k]; if (o == null || o.Do is "fire" or "move" or "draw" or "holster") return null;
            return (VerbWord(o), op.Busy != null ? CancelCostWord(k) : "");
        }

        string CancelCostWord(int k)
        {
            Activity a = null; Guard("activity", () => a = Game.ActivityOf((OpId)k)); if (a == null || !a.CanCancel) return "";
            var code = a.CancelCost ?? ""; if (CancelFree.Contains(code) || code is "quiet" or "put-down" or "keeps-vale") return "";
            if (code == "lost") { int s = Math.Max(0, (Game.Tick - Game.Ops[k].BusyStart) / Game.Hz); return s >= 1 ? DW("cancel.cost.lost", "{s} s lost").Replace("{s}", s.ToString()) : ""; }
            return DW("cancel.cost." + code, code switch { "aborted" => "They remember", "released" => "They report", "staff-report" => "Staff report", "vale-reports" => "Vale reports", "ends" => "Loop ends", _ => "" });
        }

        /// <summary>The tag over the pill before you let go (Sky 2026-10-22: never cancel unseen): "Cancels Listen · 12 s lost"; null when the
        /// order drops nothing. doers: who the order goes to.</summary>
        public string CancelTag(IEnumerable<int> doers)
        {
            if (doers == null) return null; var verbs = new List<string>(); string cost = "";
            foreach (var k in doers) if (WouldCancel(k) is var (v, c)) { if (!verbs.Contains(v)) verbs.Add(v); if (cost.Length == 0) cost = c; }
            if (verbs.Count == 0) return null; string verb = string.Join(" and ", verbs);
            return cost.Length > 0 ? DW("cancel.tagCost", "Cancels {verb} · {cost}").Replace("{verb}", verb).Replace("{cost}", cost) : DW("cancel.tag", "Cancels {verb}").Replace("{verb}", verb);
        }

        /// <summary>CancelAgent's hook (the one funnel: a new order, a walk, X, the plate's ×, a portrait drag), before the cancel: an act or a
        /// walk-in dropped pops on the agent, the plate says "cancelled", and the notes keep a row.</summary>
        void ActCancelled(int k)
        {
            if (Game == null || Ending.Length > 0) return; var op = Game.Ops[k]; var o = op.Busy ?? op.Act ?? pending[k]; if (o == null || o.Do is "fire" or "move") return;
            ActCancelPop(k, o, op.Busy != null ? CancelCostWord(k) : "");
        }

        void ActCancelPop(int k, Order o, string cost)
        {
            float now = Time.unscaledTime; string verb = VerbWord(o);
            string words = cost.Length > 0 ? DW("cancel.popCost", "{verb} cancelled · {cost}").Replace("{verb}", verb).Replace("{cost}", cost) : DW("cancel.pop", "{verb} cancelled").Replace("{verb}", verb);
            Pop(words, null, (AgentWorld(k) ?? Vector3.zero) + Vector3.up * 0.4f, Feedback.Info, DonePopS, "icons/singles/close_x");
            doneUntil[k] = now + DonePlateS * 0.7f; doneWord[k] = DW("plate.cancelled", "cancelled"); doneTone[k] = "cancel"; cancelPopAt[k] = now; actSeen[k] = null;
            DoneCancelPops++; LastCancelPop = words;
            if (!rangeMode && Game != null) { var c = NewCard("city", words, "close_x", "", AgentWorld(k), "op/" + Op.Name((OpId)k)); c.Agent = k; c.Live = false; c.Ended = Game.Tick; }
        }

        /// <summary>Engine 0.23.0: a walk-in refused at its start (she went off the street, another Watch taught it, cash spent): the refusal's
        /// short lock line over the agent.</summary>
        void DroppedPop(LogLine l)
        {
            var g = Game; if (g.Dropped.Count == 0) return; var d = g.Dropped[^1]; if (d.tick != l.Tick) return; int k = (int)d.op;
            var a = new Answer { ok = false, code = d.code, why = d.why, m = -1, cm = -1, usd = -1, tick = -1 };
            string shortLine = Text?.ShortLock(d.code) ?? d.why;
            Pop(shortLine, null, (AgentWorld(k) ?? Vector3.zero) + Vector3.up * 0.4f, Feedback.Refused, 2f, PopIcon(d.code), Humanise(d.o, a));
            if (k >= 0 && k < 4 && acts[k].Active) acts[k].Active = false;
        }
    }
}
