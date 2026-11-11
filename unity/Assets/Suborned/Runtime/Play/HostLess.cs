using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    /// <summary>Brief 014 (less reading; Glyph's briefs/014/glyph-less.md, Fable's briefs/014/fable-less.md and text.json `less`): what the play
    /// HUD needs from the host to show the run without sentences. Story beats (a card's or a scene's `beat`, at most two on screen, four seconds,
    /// a repeat within ten seconds dropped, and only when no bubble of the event's own speech is on screen), speech bubbles at the speaker (two
    /// at once, three seconds; overheard and approach scenes play every beat, call and city scenes their first three; the cell's `arrive` is
    /// history only), Vale's last known place for the plot scope (an exact place only while an agent sees him, else where he was last seen, its
    /// age growing; the briefing gives the Office), the short refusal pops (locks.*.short, less.pop) with their family glyph, and the walk, run
    /// and aim chips as parts. All of it reads the run and never changes it: a replay is untouched.</summary>
    public sealed partial class HandoverHost
    {
        // ---------------------------------------------------------------- story beats

        public sealed class StoryBeat { public string Text = "", Badge = ""; public bool Tinted; public float Born; public int Tick; public readonly HashSet<string> Speakers = new(); public bool Decided, Show; }
        /// <summary>Beats pushed this run, newest last (the HUD shows the live ones; the history keeps the cards).</summary>
        public readonly List<StoryBeat> Beats = new();
        readonly Dictionary<string, float> beatLast = new();

        void ResetLess() { Beats.Clear(); beatLast.Clear(); Bubbles.Clear(); ValeKnownTick = 0; ValeSeen = false; valeInit = false; arriveSaying = false; CellLines.Clear(); }

        /// <summary>A beat for a card or scene: dropped when the same line showed within the dedupe window; whether it shows is decided once,
        /// a moment later, when the event's own speech has had its chance to appear as a bubble in view.</summary>
        void PushBeat(string text, string badge, bool tinted, IEnumerable<string> speakers)
        {
            if (string.IsNullOrEmpty(text)) return;
            float now = Time.unscaledTime, dedupe = Text?.FeedDedupeS ?? 10f;
            if (beatLast.TryGetValue(text, out var last) && now - last < dedupe) return;
            beatLast[text] = now;
            var b = new StoryBeat { Text = text, Badge = badge, Tinted = tinted, Born = now, Tick = Game.Tick };
            if (speakers != null) foreach (var s in speakers) if (!string.IsNullOrEmpty(s)) b.Speakers.Add(s);
            Beats.Add(b); while (Beats.Count > 40) Beats.RemoveAt(0);
        }

        /// <summary>The badge a beat leads with, by the log code or scene it came from (Glyph §6).</summary>
        static (string path, bool tinted) BeatBadge(string code) => code switch
        {
            "rivals.arrive" or "rivals-arrive" or "rivals.coming" or "rivals-coming" or "rivals.drop" or "rivals-drop" => ("badges/signal_rival", false), // brief 059: the car turning in, the drop, the park
            "call" => ("badges/npc_waitress", false),
            "vale.exits" => ("badges/npc_councillor", false),
            "car.blocked" => ("icons/singles/car", true), // brief 059 K5: Vale's car stops short (car.blocks is retired with the driver's Call)
            "report" or "report.landed" => ("icons/singles/call", true),
            "security.called" or "fight.loud" => ("badges/signal_police", false),
            "alarm.dispatches/robots" => ("badges/signal_guard", false),
            "alarm.dispatches" or "alarm.dispatches/both" => ("badges/signal_police", false),
            "seal.starts" => ("icons/singles/lock", true),
            _ when code.StartsWith("report.confirmed") || code.StartsWith("stash.") => ("icons/singles/body_out", true),
            _ => ("icons/singles/dot", true),
        };

        /// <summary>The speakers of the text's events that a log code belongs to (whose bubble, in view, says the beat instead).</summary>
        List<string> SpeakersOf(string code)
        {
            var l = new List<string>(); if (Text == null) return l;
            foreach (var e in Text.Events.Values) foreach (var lc in e.Log) if (code == lc || (lc.EndsWith(".") && code.StartsWith(lc))) { l.AddRange(e.Speakers); break; }
            return l;
        }

        /// <summary>A card from text.json for a log code was made: its beat, if it has one (a pick key chooses among several).</summary>
        void CardBeat(PlayText.CardText c, LogLine l, Person who)
        {
            string beat = c.Beat;
            string pick = "";
            if (c.Beats.Count > 0) { pick = CardPick(l, who); if (!c.Beats.TryGetValue(pick, out beat)) beat = null; }
            if (string.IsNullOrEmpty(beat)) return;
            var (badge, tinted) = BeatBadge(pick.Length > 0 ? $"{l.Code}/{pick}" : l.Code);
            PushBeat(beat, badge, tinted, SpeakersOf(l.Code));
        }

        /// <summary>A scene that carries a beat (the rivals arriving, Inez's call): pushed with the scene's speakers.</summary>
        void SceneBeat(PlayText.Scene sc, string code)
        {
            if (sc?.Beat == null) return;
            var sp = new List<string>(); foreach (var v in sc.Variants) foreach (var b in v) sp.Add(b.Speaker);
            var (badge, tinted) = BeatBadge(code);
            PushBeat(sc.Beat, badge, tinted, sp);
        }

        /// <summary>Per frame (the HUD asks): each beat is decided once, a quarter second after it came, against the bubbles then on screen.</summary>
        public void DecideBeats(Camera view)
        {
            float now = Time.unscaledTime;
            foreach (var b in Beats)
            {
                if (b.Decided || now - b.Born < 0.25f) continue;
                b.Decided = true; b.Show = true;
                foreach (var bub in Bubbles) if (b.Speakers.Contains(bub.Speaker) && now - bub.Born < (Text?.BubbleS ?? 3f) && BubbleWorld(bub) is Vector3 w && OnScreen(view, w)) { b.Show = false; break; }
            }
        }

        static bool OnScreen(Camera c, Vector3 w) { if (!c) return false; var v = c.WorldToViewportPoint(w); return v.z > 0 && v.x > 0.02f && v.x < 0.98f && v.y > 0.02f && v.y < 0.98f; }

        // ---------------------------------------------------------------- speech bubbles

        public sealed class Bubble { public string Speaker = "", Text = ""; public float Born, Life = 3f; public int Tick; public bool Inset; }
        /// <summary>Lines spoken this run that may show as bubbles (the HUD shows the live ones whose speaker is in view).</summary>
        public readonly List<Bubble> Bubbles = new();
        bool arriveSaying;

        void PushBubble(string speaker, string text)
        {
            if (arriveSaying || string.IsNullOrEmpty(speaker) || string.IsNullOrEmpty(text) || speaker is "crowd" or "watcher" or "cell") return;
            var bub = new Bubble { Speaker = speaker, Text = text, Born = Time.unscaledTime, Life = Text?.BubbleS ?? 3f, Tick = Game?.Tick ?? 0, Inset = GuardOnACall(speaker) };
            Bubbles.Add(bub); if (bub.Inset) SpeechInset(bub); // brief 085: the guard's lines on any Call, off screen, in a docked inset (HostConvo.Street)
            while (Bubbles.Count > 12) Bubbles.RemoveAt(0);
        }

        /// <summary>A scene's lines as they are revealed (the feed reveals them by the act's progress or the clock): overheard and approach scenes
        /// every beat, call and city scenes their first few (less.feed.cityBeats).</summary>
        void SceneBubbles()
        {
            int most = Text?.CityBeats ?? 3;
            foreach (var c in Feed)
            {
                if (c.Scene == null) { c.Bubbled = c.Revealed; continue; }
                bool few = c.Scene.Kind is "call" or "city";
                for (; c.Bubbled < Math.Min(c.Revealed, c.Lines.Count); c.Bubbled++) if (!few || c.Bubbled < most) PushBubble(c.Lines[c.Bubbled].Speaker, c.Lines[c.Bubbled].Text);
            }
        }

        /// <summary>Where a speaker's bubble sits: over their head while their view is on the street, else nowhere (the beat speaks for them).</summary>
        public Vector3? BubbleWorld(Bubble b)
        {
            if (b.Speaker == "milena" && Game != null && views?.ListenPartnerHead() is Vector3 lph) return lph; if (b.Speaker == "milena" && Game != null) return frame.World(Game.S.At(Game.S.GuardCafe)) + Vector3.up * 2.1f; // brief 085: a voice through the café door, with no figure
            string key = b.Speaker switch
            {
                "scout" or "fixer" or "tech" or "enforcer" => "op/" + b.Speaker, "rivals" => views?.RivalAnchorKey() ?? "car/rivals", "security" => "security/0", "patrol" => "patrol",
                "waitress" or "driver" or "guard" or "councillor" => b.Speaker, _ => null,
            };
            if (key == null || views == null) return null;
            var t = views.Find(key); if (!t || !t.gameObject.activeSelf) return null;
            if (b.Speaker.Length > 0 && Array.IndexOf(OpKeys, b.Speaker) is int k && k >= 0 && Game.Ops[k].Inside) return null;
            return t.position + Vector3.up * 2.35f;
        }

        // ---------------------------------------------------------------- Vale on the plot scope

        /// <summary>Where the cell last knew Vale to be (world), when (tick), and whether an agent sees him now (an exact place) — the briefing
        /// gives the Office at the start. Told and learned (GDD §04): the scope never shows him where no agent has seen him.</summary>
        public Vector3 ValeKnown { get; private set; }
        public int ValeKnownTick { get; private set; }
        public bool ValeSeen { get; private set; }
        bool valeInit;

        void TrackVale()
        {
            if (Game == null) return;
            if (!valeInit) { valeInit = true; ValeKnown = frame.World(Game.S.At(Game.S.GuardPost)); ValeKnownTick = 0; }
            Pt? at = Game.Vale == ValeAt.WithEnforcer ? Game.Ops[(int)OpId.Enforcer].Pos : Game.ValePos();
            bool seen = Game.Vale == ValeAt.WithEnforcer || (at.HasValue && CellSees(at.Value));
            ValeSeen = seen && at.HasValue;
            if (ValeSeen) { ValeKnown = frame.World(at.Value); ValeKnownTick = Game.Tick; }
        }

        // ---------------------------------------------------------------- short pops (Fable §4, Glyph §6)

        /// <summary>The family glyph for a refusal code: reach, watch first, listen first, the agent (portrait), money, a window, guns, bodies,
        /// doors, else the slashed circle.</summary>
        public static string PopIcon(string code) => code switch
        {
            "too-far" or "out-of-range" or "needs-reach" or "no-place" or "unwalkable" or "no-walk" or "no-way" => "icons/singles/s_moving",
            _ when code.StartsWith("stand-") && code != "stand-shut" => "icons/singles/s_moving",
            "needs-routine" => "icons/singles/watch",
            "needs-need" or "needs-temperament" or "no-leverage" => "icons/singles/listen",
            "only-scout" => "portraits/cameo_scout_normal", "only-fixer" => "portraits/cameo_fixer_normal", "only-tech" => "portraits/cameo_tech_normal", "only-enforcer" => "portraits/cameo_enforcer_normal",
            "no-cash" => "icons/singles/pay",
            "driver-too-late" or "call-too-late" or "car-busy" or "patrol-away" or "agent-busy" or "summon-too-far" => "icons/singles/p_time",
            "no-weapon" or "already-drawn" or "not-drawn" or "fire-carrying" or "no-target" or "civilian-target" or "no-clear-line" or "target-unknown" or "fire-on-cell" or "fire-carrying-vale"
                or "stance-unknown" or "panic-not-cell" or "panic-no-fight" or "panic-nobody" or "unarmed" or "never-vale" or "no-line" or "stance-hold" or "stance-return" or "stance-free" => "icons/singles/fire",
            "carry-vale" or "already-carrying" or "carry-names-downed" or "carry-self" or "not-down" or "no-body" or "already-carried" or "body-stashed" or "not-carrying" or "stash-full" or "body-dead"
                or "already-restrained" or "keeps-vale" or "run-carrying-vale" or "run-carrying-downed" or "other-carries" or "no-room" => "icons/singles/s_carrying",
            "doors-locked" or "door-held" => "icons/singles/lock",
            // Glyph 2026-10-26 §5 (Drive's refusals): the fixable hold is the lock; the fight that has started is the car struck through
            "drive-held" => "icons/singles/lock", "drive-fight" => "icons/singles/car#struck", "drive-busy" or "stand-shut" => "icons/singles/p_time",
            "drive-no-driver" or "drive-here" or "driver-brings-vale" => "icons/singles/car", "unknown-stand" => "badges/mark_no", "car-not-parked" => "icons/singles/p_time", // brief 059 K3: the Take round the back
            "car-no-driver" => "icons/singles/car", // engine 0.34.0 (M067-K3 c): Go with only the downed aboard
            "draw-first" => "icons/singles/fire", "car-wheelman" => "portraits/cameo_enforcer_normal", // engine 0.53.0 (brief 105, car.take): Take car's fixes
            "car-disabled" or "car-full" or "car-taken" or "car-ours" or "car-moving" or "car-driver" or "car-crewed" or "car-unknown" => "icons/singles/car",
            "hints-on" or "hints-off" => "icons/singles/p_unknown",
            _ => "badges/mark_no",
        };

        /// <summary>A refusal's pop: the code's short form with {m} {usd} {tick} filled (as the long line), else the host's own short.</summary>
        string ShortOf(Order o, Answer a)
        {
            var s = BoxedShort(a) ?? Text?.ShortLock(a.code, LockVariantFor(o, a)) ?? HostShort(a.code); // brief 102: tie-unknown before the cell knows the pair (HostCall) // brief 098 step 1b: "Boxed in · red car" (HostTrafficBoxed)
            if (s == null) return null;
            string m = a.m >= 0 ? a.m.ToString() : a.cm >= 0 ? Math.Round(a.cm / 100.0, MidpointRounding.AwayFromZero).ToString() : null; // brief 108: none drops its clause, never "?"
            string usd = a.usd >= 0 ? "$" + a.usd.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : null;
            string tick = a.tick >= 0 ? LockTick(a.code, a.tick) : null;
            s = Clause.Fill(Clause.Fill(Clause.Fill(s, "{m}", m), "{usd}", usd), "{tick}", tick); if (s.Length == 0) return null;
            s = s.Replace("{holders}", Text?.HoldersPhrase(a.ids) ?? "hostiles").Replace("{op}", o.Op.Length > 0 && IsOpName(o.Op) ? AgentNames[(int)Op.Parse(o.Op)] : "The agent");
            // Sky, route B: too early for Take says when Vale comes to his car (the briefing's timeline mark)
            if (a.code == "vale-not-at-car" && Text?.Timeline != null) foreach (var (at, label, _) in Text.Timeline) if (label.Contains("reaches his car")) { s += " · " + at; break; }
            return s;
        }

        /// <summary>The host's own short for an engine refusal Fable's text does not word yet (a pop is never the engine's sentence); null for the rest.
        /// Engine 0.34.0: car-no-driver (Go with only the downed aboard: Motive M067-K3 c).</summary>
        static string HostShort(string code) => code switch { "car-no-driver" => "No driver aboard", "draw-first" => "Draw first", "car-wheelman" => "Enforcer only", "car-disabled" => "Shot out", _ => null };

        /// <summary>A port of text.schema.ts `lockVariant` (engine 0.33.0, Fable): car-held with the rival car among its holders (Answer.ids) is
        /// worded by `rival-car` when the engine sends its first rider's step-out tick, `rival-car-driving` when it sends none (-1: the car still
        /// driving in); any other answer keeps the caller's (the target's kind: "machine", or null for the person case).</summary>
        static string LockVariant(Answer a, string targetKind)
        {
            if (a.code != "car-held" || string.IsNullOrEmpty(a.ids)) return targetKind;
            foreach (var id in a.ids.Split(',')) if (id.Trim() == "car:rivals") return a.tick >= 0 ? "rival-car" : "rival-car-driving";
            return targetKind;
        }
        /// <summary>{tick} in a lock's words (text.schema.ts tickAs): the mission clock ("6:13"), or for a lock marked `tickAs: left` the seconds
        /// still to wait from now ("18 s": ceil, never below 0), as ctx.left says it.</summary>
        string LockTick(string code, int tick)
        {
            if (Game != null && Text?.LockByCode(code ?? "") is PlayText.LockText l && l.TickAs == "left") return $"{Math.Max(0, (tick - Game.Tick + Game.Hz - 1) / Game.Hz)} s";
            return MissionClock(tick);
        }
        /// <summary>The getaway car's holder chip when the rival car holds it (engine 0.33.0, Fable's fillLockShort): "Rivals coming · 18 s" with
        /// the step-out tick, "Rivals coming" while it still drives in; "" when the rival car holds nothing. Asked once a tick.</summary>
        public string RivalsHoldShort()
        {
            var g = Game; if (g == null || g.Range) return "";
            if (rivalsHoldTick == g.Tick && rivalsHoldOrders == g.Orders.Count) return rivalsHold;
            rivalsHoldTick = g.Tick; rivalsHoldOrders = g.Orders.Count; rivalsHold = "";
            var a = GoLock(); if (a.code != "car-held" || !(a.ids ?? "").Contains("car:rivals")) return rivalsHold;
            rivalsHold = ShortOf(new Order { Op = Op.Name(Agent), Do = "go" }, a) ?? (a.tick >= 0 ? $"{Math.Max(0, (a.tick - g.Tick + Game.Hz - 1) / Game.Hz)} s" : "");
            return rivalsHold;
        }
        int rivalsHoldTick = -1, rivalsHoldOrders = -1; string rivalsHold = "";

        /// <summary>Fable 019's `byTarget` key for an order's target: "machine" for a camera (by its id or "camera:" key), the patrol robot or a
        /// response robot; "crew" for one of the rival crew (brief 096); else null (the base line, the person case).</summary>
        string TargetKindOf(string target)
        {
            if (string.IsNullOrEmpty(target)) return null;
            if (target == "patrol" || target.StartsWith("camera:") || target.StartsWith("camera/") || target.Contains("robot")) return "machine";
            if (IsCrew(target)) return "crew"; // brief 096 (Fable): one of the rival crew, worded by byTarget.crew (needs-routine: one Watch on any of them)
            foreach (var p in Pickables()) if (p.T.Kind == "camera" && p.T.Key == target) return "machine";
            return null;
        }

        /// <summary>A pop the host builds itself (less.pop by key), with its long line kept for Alt.</summary>
        void PopKey(string key, Vector2? screen, Vector3? world, Feedback kind, string longLine = "", string fill = null)
        {
            string s = Text != null && Text.Pops.TryGetValue(key, out var p) ? p : Text?.ShortLock(key) ?? key;
            if (fill != null) s = s.Replace("{op}", fill).Replace("{m}", fill);
            s = s.Replace("{m}", "?");
            popCode = key; Pop(s, screen, world, kind, 2f, PopIcon(key), longLine);
        }

        /// <summary>Where the camera opens a run (block cm; Aster/Waymark 2026-10-16): Unity x -170.5, z -227.5.</summary>
        public static readonly Pt OpeningFocus = new(-250, -4050);

        // ---------------------------------------------------------------- Standing (brief 014; Keel's engine 0.6.0, Fable's debrief.standing)

        /// <summary>The debrief's Standing lines in words: "{district}: {band} {to}, was {from}. {cause}", from the text, filled with the signed
        /// numbers, the district's name and who; with the move's direction (+1 up, -1 down, 0 none) for the end card's small arrow.</summary>
        public List<(string line, string band, int to, int dir)> StandingLines()
        {
            var o = new List<(string, string, int, int)>(); var d = EndDebrief; if (d == null || Text == null) return o;
            foreach (var l in d.Next.Standing)
            {
                string Signed(int v) => v > 0 ? $"+{v}" : v < 0 ? $"\u2212{-v}" : "0";
                string band = Text.StandingBands.TryGetValue(l.Band, out var b) ? b : l.Band;
                string cause = Text.StandingCauses.TryGetValue(l.Cause, out var c) ? c.Replace("{who}", l.Who.Length > 0 ? Name(l.Who) : "someone") : "";
                string district = string.Join(" ", Array.ConvertAll(l.District.Split('-', ' '), w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));
                var line = Text.StandingLine.Replace("{district}", district).Replace("{band}", band).Replace("{to}", Signed(l.To)).Replace("{from}", Signed(l.From)).Replace("{cause}", cause).Trim();
                o.Add((line, band, l.To, Math.Sign(l.To - l.From)));
            }
            return o;
        }

        // ---------------------------------------------------------------- walk and run (Sky 2026-10-16: "running looks the same as walking")

        /// <summary>A moving agent between walk and run toward the same point: the move order again, to the walk's own end point, at the other
        /// pace (an engine order, recorded, so a replay is exact). The engine's refusals stand (no running with a body or Vale): its pop, and the
        /// walk goes on unchanged. An act the agent walks for (walk-then-act) still follows on arrival.</summary>
        public void ToggleGait(int k)
        {
            if (Game == null || Game.Over || Fault != null || !ModalGate.AllowsClicks(Modal)) return;
            var op = Game.Ops[k]; if (op.Walk == null || op.Inside || op.Down) return;
            var keep = pending[k]; var keepAt = pendingAt[k]; var keepFrom = pendingFrom[k];
            bool ok = Issue(new Order { Op = Op.Name((OpId)k), Do = "move", Point = op.Walk.End, Run = !op.Running }, null, SelectedWorldOf(k));
            if (ok) { pending[k] = keep; pendingAt[k] = keepAt; pendingFrom[k] = keepFrom; gaitSeenAt[k] = -1f; }
        }

        /// <summary>G: every selected agent that is moving.</summary>
        void ToggleGaitSelected() { for (int k = 0; k < 4; k++) if (Selected[k] && Game.Ops[k].Walk != null) ToggleGait(k); }

        Vector3? SelectedWorldOf(int k) => frame.World(Game.Ops[k].Pos);

        /// <summary>A running agent's eye (who could see the run now, the engine's card-only SeenQuote along the rest of the route), refreshed at
        /// most twice a second: "shut", "open", "unk"; "" while walking (a walk is not noticed).</summary>
        public string RunEye(int k)
        {
            var op = Game.Ops[k]; if (!op.Running || op.Walk == null) return "";
            if (Time.unscaledTime - gaitSeenAt[k] < 0.5f) return gaitEye[k];
            gaitSeenAt[k] = Time.unscaledTime; MoveQuote q = null;
            Guard("run sight", () => q = Game.QuoteMove(Op.Name((OpId)k), op.Walk.End));
            return gaitEye[k] = q == null || q.RunFree ? "" : CrowdGuardSees(q) ? "open" : EyeOf(q.Seen);
        }
        /// <summary>Engine 0.22.0 (next-batch item 4): a guard walking the street (a security-staff person, not a watcher with a cone) would see
        /// the run now: MoveQuote.RunSeenBy lists him by person id. He is in plain sight, so the run's price counts him (SeenQuote does not).</summary>
        public bool CrowdGuardSees(MoveQuote q) { if (q == null || Game == null) return false; foreach (var id in q.RunSeenBy) if (!id.StartsWith("camera:") && id is not ("guard" or "patrol" or "rivals") && !id.StartsWith("security") && Game.PersonById(id) != null) return true; return false; }
        readonly float[] gaitSeenAt = { -1f, -1f, -1f, -1f }; readonly string[] gaitEye = { "", "", "", "" };

        // ---------------------------------------------------------------- the chips as parts (Glyph §2: icons and numbers)

        /// <summary>The walk-or-run chip on empty ground: walk and run seconds, the run's eye (shut, open, ?) and its Alert; refused: the pop's
        /// words. Carrying: the carry's seconds and its eye.</summary>
        public sealed class MoveParts
        {
            public int WalkS = -1, RunS = -1, CarryS = -1; public string Eye = "", Alert = "", Refused = ""; public bool RunFree;
            /// <summary>Engine 0.31.0 (brief 054 §3.5, Fable's price `eye`): the passers-by who would keep the runner's face (MoveQuote.RunRememberedBy:
            /// Alert 2 or more, a quiet run); the card's eye mark carries their number. 0 none.</summary>
            public int Remembers;
            /// <summary>The move preview (Glyph, "Move preview", variant A): where the ring sits, its state (safe, seen, unk, alert), the seeing
            /// watcher's kind (camera, guard, rivals: the ring's colour), the stretches of the route inside each known watcher's sight (the glow,
            /// world points with the watcher's kind), and the unwatched watchers on the route (a faint "?" over each).</summary>
            public Vector3 At; public string State = "safe", SeenKind = "";
            public readonly List<(Vector3 a, Vector3 b, string kind)> Glow = new(); public readonly List<Vector3> Unwatched = new();
            /// <summary>Brief 027 B5 (lab, cover snap): the move ends on a cover point instead of the cursor's ground. At is moved to the point;
            /// SnapFrom keeps the cursor's ground for the tether; SnapId names the point (its mark lights). Null / "" when it doesn't snap.</summary>
            public Vector3? SnapFrom; public string SnapId = "";
            /// <summary>Engine 0.35 (Glyph b-final, "when cover marks show"): the snapped end flanks the focus's cover (Game.PreviewMoveSnap: the focus
            /// holds a cover that no longer counts from that end): where that cover's kneel point is, for one struck shield disc. Null when not.</summary>
            public Vector3? FlankAt;
        }

        /// <summary>The ring's state and the route's glow for a walk from agent k to `to` (the engine's card-only SeenAt per route sample; read-only).</summary>
        void PreviewRoute(int k, Pt to, MoveParts mp, SeenQuote whole, bool carrying)
        {
            var g = Game; mp.At = frame.World(to);
            string Pri(IEnumerable<string> ids) { string best = ""; int rank = 9; foreach (var w in ids) { var kind = Game.WatcherKind(w); int r = kind == "camera" ? 0 : kind is "guard" or "security" or "patrol" ? 1 : kind == "rivals" ? 2 : 3; if (r < rank) { rank = r; best = kind == "security" || kind == "patrol" ? "guard" : kind; } } return best; }
            mp.SeenKind = whole == null ? "" : Pri(whole.SeenBy);
            mp.State = whole == null || whole.Seen == "no" ? "safe" : whole.Seen == "?" ? "unk" : carrying ? "alert" : "seen";
            if (whole != null) foreach (var w in whole.MaybeSeenBy) if (Game.WatcherKind(w) != "camera" && WatcherAt(w) is Vector3 wp) mp.Unwatched.Add(wp + Vector3.up * 3f); // brief 101: an unwatched camera wears its own "?" always
            if (whole == null || whole.SeenBy.Count == 0) return;
            var route = MoveRoute(g.Ops[k].Pos, to); if (route == null) return; // engine 0.33.1: round the standing cars
            var pts = new List<Pt>(); for (int i = 1; i < route.Length; i++) { Pt a = route[i - 1], c = route[i]; int n = Math.Max(1, G.Dist(a, c) / 60); for (int j = 0; j < n; j++) pts.Add(G.Lerp(a, c, j, n)); } pts.Add(route[^1]);
            string prev = ""; Pt from = pts[0];
            for (int i = 0; i < pts.Count; i++)
            {
                var q = g.SeenAt(pts[i]); string kind = q.Seen == "yes" ? Pri(q.SeenBy) : "";
                if (kind != prev) { if (prev.Length > 0) mp.Glow.Add((frame.World(from), frame.World(pts[i]), prev)); from = pts[i]; prev = kind; }
            }
            if (prev.Length > 0) mp.Glow.Add((frame.World(from), frame.World(pts[^1]), prev));
        }

        /// <summary>Where a watcher stands now (world), for the unwatched "?".</summary>
        Vector3? WatcherAt(string id)
        {
            var g = Game;
            switch (Game.WatcherKind(id))
            {
                case "camera": { var c = g.S.Cameras.Find(x => "camera:" + x.Id == id); return c == null ? null : frame.World(c.At) + Vector3.up * 0.6f; }
                case "guard": return g.GuardPose() is var gp && gp.HasValue ? frame.World(gp.Value.pos) : null;
                case "patrol": return g.PatrolPose() is var pp && pp.HasValue ? frame.World(pp.Value.pos) : null;
                case "rivals": return g.PosOf(id) is Pt rp ? frame.World(rp) : null; // brief 059 K8: each member is a watcher (RivalsPose is the migration shim)
            }
            return null;
        }
        public MoveParts MoveNow { get; private set; }

        /// <summary>The crosshair chip: the target's badge, down in n s, hits as a fraction, Trail per hit; never: the refusal.</summary>
        public sealed class AimParts { public string Badge = "", Down = "", Ratio = "", Trail = "", Refused = ""; public bool Civilian, Cover, Lab; public int WalkS = -1, Charge, Exposed; public string Band = ""; /** brief 027: Down is pips · −damage · ×hits (no seconds) */ }
        public AimParts AimNow { get; private set; }

        /// <summary>A watcher-or-not eye for a SeenQuote: "shut" nobody, "open" a known watcher sees it, "unk" an unknown one could.</summary>
        public static string EyeOf(SeenQuote q) => q == null || q.Seen == "no" ? "shut" : q.Seen == "?" ? "unk" : "open";
    }
}
