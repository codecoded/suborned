using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>What the HUD shows, as plain data (step 1 of the UI Toolkit move, Latch's plan approved by Aster 2026-10-15): the host and the
    /// engine fill it; a view draws it. Today's view is IMGUI (PlayHud); the UI Toolkit view will read the same model, so the kit drops in
    /// without touching the host or the engine bindings. No Unity objects in here: world positions are Vector3 (a view projects them), and
    /// strings are built once per change (Refresh, on a tick, an order, a log line, pause or speed), or per frame for what follows the
    /// cursor and the camera (Frame). Clicks are not here: a view calls the host's own methods, as the IMGUI buttons do.</summary>
    public sealed partial class HudModel
    {
        public sealed class Slot
        {
            public string Key = "", Name = "", Stance = "", Glyph = "", Label = "", Left = "", Under = "", CancelTip = "", Phase = "";
            public bool Selected, HintRing, Armed, Drawn, Down, Busy, CanCancel; public int Hp, HpMax, Progress = -1;
            // for the kit view (Glyph's slot strip): role, the atlas icon, the state tag, the ready/hurt/down line, the queued act, the cancel tooltip
            public string Role = "", Icon = "", StateTag = "", HintLine = "", Queued = "", CancelTitle = "", CancelBody = "", Pace = ""; public bool Hurt, Show, Compact;
            /// <summary>Brief 018 §6.6: in the getaway car (the roster's in-car badge); while the Go card is up, the fate of one a Go would leave ("captured" or "walked", else "").</summary>
            public bool InCar; public string LeftFate = "";
        }
        public sealed class AgentMark { public Vector3? World; public string Num = "", Verb = ""; public bool Selected; }
        public sealed class CastIcon { public Vector3? World; public string A = "", B; }
        public sealed class Ring { public bool Show, Selected, CanCancel; public Vector3 World; public string Glyph = "", Tip = "", ActGlyph = ""; public int Progress = -1; public Vector3? ActAt;
            /// <summary>Glyph techwatch fix 3: a timed act that ran to its end keeps its ring a beat (DoneSeconds), full, with a tick. Done: showing that beat now.</summary>
            public bool Done; internal bool WasActing; internal int LastEnd = -1; internal float DoneAt = -10f; }
        /// <summary>How long a finished act's ring stays, full, with its tick (unscaled seconds: it holds through a pause).</summary>
        public const float DoneSeconds = 1.2f;
        public struct Pips { public Vector3 World; public int Hp, Max; public bool Cell; }
        public struct Downed { public Vector3 World; public string Line; }
        /// <summary>One person in Intel (Glyph's intel_panel): what the cell knows of them, and the secret and the lover tie only once known.</summary>
        public sealed class IntelRow { public string Role = "", Name = "", Job = "", NeedIcon = "p_unknown", NeedMark = "unk", TempIcon = "p_unknown", Next = "", NextIcon = "", Secret = ""; public bool Turned, Known; }
        public readonly IntelRow[] Intel = { new(), new(), new(), new() }; public int IntelKnown; public bool IntelOpen;
        public sealed class PanelCard
        {
            public string Key = "", Title = "", Does = "", Price = "", Lock = "", Verb = "";
            // the kit's card: icon, the four price parts, the outcome and its mark, the walk, the lock reason and what unlocks it
            public string Icon = "", Usd = "", Time = "", Trail = "", Alert = "", Outcome = "", OutcomeMark = "unk", Walk = "", Reason = "", Unlock = "", Role = "", Name = "";
            public bool Available, Mine, Leverage, Suggested;
            /// <summary>Brief 013: the card's own rows under the price (a takedown way's found, never found, wakes; a loop's blinds).</summary>
            public readonly List<(string icon, string text)> Extra = new();
        }
        public string PanelRole = "", PanelTarget = "", PanelBadge = "", PanelSub = "", PanelNeed = "", PanelNeedMark = "", PanelTemp = ""; public bool PanelPerson, PanelSecret;
        // the price chip's parts (the kit's chip_price: verb icon, $ · time · Trail · Alert, the outcome mark)
        public string ChipIcon = "", ChipUsd = "", ChipTime = "", ChipTrail = "", ChipAlert = "", ChipMark = "unk", ChipSeen = "";
        /// <summary>Over the watchers noticing an agent whose sight the cell doesn't know: a "?" (world points).</summary>
        public IReadOnlyList<Vector3> Unknowns = System.Array.Empty<Vector3>();
        // the briefing as the kit's card lays it out
        public string BriefTitle = "", BriefSub = "", BriefSituation = "", BriefObjective = "", BriefWin = "", BriefClock = "", BriefPurse = "", BeginKey = "", RestartKey = "";
        public readonly List<string> BriefFails = new(); public readonly List<(string title, string body)> BriefWays = new();
        public enum ChipKind { None, Price, Hint, Aim, AimWarn, Move }
        public enum Part { Title, Body, Small, Footer }

        // the frame
        public string Fault, Status = "";
        public float Scale = 1f;
        public RosterDock Dock;
        // the pill
        public string Clock = "", Next = "", Cash = "", Trail = "", State = "", City = "", Tracker = ""; public int Alert;
        // the roster and hotbar
        public readonly Slot[] Slots = { new(), new(), new(), new() };
        public readonly string[] VerbGlyph = new string[4], VerbKey = new string[4], VerbIcon = new string[4], VerbName = new string[4]; public readonly bool[] VerbHas = new bool[4], VerbRefused = new bool[4]; /* a verb the agent has but can't give now (Fire while carrying Vale): its knob greys, its glyph kept */ public int VerbLit, Agent; public string VerbCaption = "";
        public bool AllStrips, Fight; public string NextLabel = "", NextTime = "", Objective = "", ObjectiveTail = "", CancelKey = "";
        // marks in the world
        public readonly AgentMark[] Marks = { new(), new(), new(), new() };
        public readonly CastIcon[] Cast = { new(), new(), new(), new() };
        public readonly Ring[] Rings = { new(), new(), new(), new() };
        public readonly List<Pips> Fighters = new(); public readonly List<Downed> Down = new();
        public Vector3? HoverRing; public int Cursor; // 0 default, 1 act, 2 crosshair, 3 crosshair with no line
        // the chip at the cursor
        public ChipKind Chip; public string ChipText = ""; public Vector2 Mouse;
        // cards
        public bool PanelOpen; public string PanelTitle = "", PanelAbout = "", PanelNote = ""; public Vector2 PanelAt; public readonly List<PanelCard> PanelCards = new();
        public bool LoudOpen; public readonly List<string> LoudLines = new(); public float LoudProgress; public Vector2 LoudAt;
        public string Hint, HintKey = ""; public Vector3? HintRing; public string Toast;
        public bool BriefingOpen; public readonly List<(string text, Part part)> Briefing = new();
        public bool Ended; public readonly List<string> EndLines = new();
        public Rect? Box;
        // the feed (its cards are the host's plain data) and the ticker
        public int Unread; public bool FeedFold, HistoryOpen; public string Filter = "all", AckKey = "", HistoryKey = "";
        public IReadOnlyList<HandoverHost.FeedCard> Feed = System.Array.Empty<HandoverHost.FeedCard>();
        public readonly List<(string text, HandoverHost.FeedCard card, bool unread)> History = new();
        public HandoverHost.Said Ticker; public float TickerAge;
        public IReadOnlyList<HandoverHost.PopUp> Pops = System.Array.Empty<HandoverHost.PopUp>();
        public IReadOnlyList<(Vector3 world, HandoverHost.Feedback kind, float born)> Pulses = System.Array.Empty<(Vector3, HandoverHost.Feedback, float)>();

        static readonly string[] Names = { "Scout", "Fixer", "Tech", "Enforcer" }, Nums = { "1", "2", "3", "4" }, Roles = { "waitress", "driver", "guard", "councillor" };
        int refreshKey = int.MinValue; HandoverHost.ActionPanel cardsFor; string historyKey = "";

        /// <summary>Everything that changes at most once a tick, on an order or a log line, or with pause and speed.</summary>
        public void Refresh(HandoverHost h)
        {
            var g = h.Game; int key = unchecked(g.Tick * 31 + g.Orders.Count * 7919 + (h.Paused ? 1 : 0) + h.Speed * 3 + g.Log.Count * 104729 + (h.Ending.Length > 0 ? 17 : 0));
            if (key == refreshKey) return;
            refreshKey = key;
            Clock = HandoverHost.MissionClock(g.Tick); Next = NextOnTimeline(h);
            { var (lbl, at) = NextParts(h); NextLabel = lbl; NextTime = at; }
            Cash = $"${g.Cash:N0}"; Trail = $"T {g.Trail}"; Alert = g.Alert; State = h.Paused ? "<color=#ffd27a>PAUSE</color>" : $"x{h.Speed}";
            var c = h.controls;
            City = $"{h.CityClock()} · {c.Label(PlayAction.Pause)} pause · {c.Label(PlayAction.Briefing)} briefing · {c.Label(PlayAction.DialogueLog)} history · {c.Label(PlayAction.Cancel)} cancel · N next";
            Tracker = h.Text == null ? "" : "▸ " + h.Text.Tracker.Replace("{close}", h.Text.Close);
            if (h.Text != null) { var tr = h.Text.Tracker.Replace("{close}", h.Text.Close); int cut = tr.LastIndexOf(" · ", System.StringComparison.Ordinal); Objective = cut > 0 ? tr[..cut] : tr; ObjectiveTail = cut > 0 ? "  ·  " + tr[(cut + 3)..] : ""; }
            for (int i = 0; i < 4; i++) { var (a, b) = Icons(g, Roles[i]); Cast[i].A = a; Cast[i].B = b; }
            IntelKnown = 0;
            for (int i = 0; i < 4; i++)
            {
                var p = g.PersonByRole(Roles[i]); var r = Intel[i];
                r.Role = Roles[i]; r.Name = h.Text?.TargetName(Roles[i], p.Name) ?? p.Name; r.Name = r.Name.Length > 0 ? char.ToUpperInvariant(r.Name[0]) + r.Name[1..] : r.Name;
                r.Job = Roles[i] switch { "waitress" => "café waitress", "driver" => "Vale's driver", "guard" => "door guard", _ => "the councillor" };
                r.NeedIcon = p.KNeed ? p.Need : p.Suspected.Length > 0 ? p.Suspected : "p_unknown"; r.NeedMark = p.KNeed ? "ok" : p.Suspected.Length > 0 ? "sus" : "unk";
                r.TempIcon = p.KTemperament ? p.Temperament : "p_unknown"; r.Turned = p.Turned;
                r.Known = p.KRoutine || p.KNeed || p.KTemperament || p.Turned || p.Suspected.Length > 0; if (r.Known) IntelKnown++;
                // the secret, and the lover tie it reveals, only once the cell knows it (Listen, or a Watch of the pair together)
                r.Secret = p.Knows("secret") ? (p.Secret == "affair" && p.Lover != null ? $"Secret: seeing {h.Text?.TargetName(p.Lover.Role, p.Lover.Name) ?? p.Lover.Name}" : p.Secret == "record" ? "Secret: a record" : $"Secret: {p.Secret}") : "";
                (r.Next, r.NextIcon) = Roles[i] == "councillor" ? (g.Vale == ValeAt.WithEnforcer ? ("with the Enforcer", "take") : ("Take him at his car", "take"))
                    : p.Turned ? ("Call them when it counts", "call") : !p.KRoutine ? ("Watch: learn the routine", "watch") : !p.KNeed ? ("Listen: learn the need", "listen") : ("Turn: Ask, Pay, Help or Pressure", "help");
            }
            if (h.Ending.Length > 0) EndCard(h); else Ended = false;
        }

        /// <summary>Per frame: what follows the camera, the cursor and the selection; the roster's live state; the open cards.</summary>
        int builtFrame = -1; List<(string op, string fate, string why)> goLeft; static readonly List<(string op, string fate, string why)> noneLeft = new();
        public void Frame(HandoverHost h)
        {
            if (builtFrame == Time.frameCount) return; builtFrame = Time.frameCount; // both views may ask; it is built once a frame
            var g = h.Game; Refresh(h);
            goLeft = h.GoCardOpen ? h.LeftBehindNow() : noneLeft; // brief 018: the Go card's left-behind flags on the roster
            Fault = h.Fault != null ? h.Status : null; Status = h.Status; Dock = h.controls.dock; Mouse = h.Mouse; Box = h.BoxScreen;
            AckKey = h.controls.Label(PlayAction.Acknowledge); HistoryKey = h.controls.Label(PlayAction.DialogueLog); HintKey = h.controls.Label(PlayAction.Hints);
            bool ended = h.Ending.Length > 0;
            // the roster
            for (int i = 0; i < 4; i++)
            {
                var s = Slots[i]; var op = g.Ops[i]; var d = h.DoingOf(i);
                s.Key = h.AgentKeyLabel(i); s.Name = Names[i]; s.Selected = h.Selected[i]; s.HintRing = h.HintNow != null && h.HintNow.Portrait == i;
                s.Hp = op.Gun.Health; s.HpMax = Mathf.Max(op.Gun.Max, op.Gun.Health); s.Armed = op.Gun.W != null; s.Drawn = op.Gun.Drawn && op.Gun.HolsterAt < 0;
                s.Stance = op.Gun.Stance == "hold" ? "H" : op.Gun.Stance == "free" ? "A" : "R"; s.Down = op.Down;
                s.Busy = d.Busy; s.Phase = d.Phase; s.Glyph = d.Phase is "walking" or "running" ? (d.Phase == "running" ? "»" : "›") : d.Glyph; s.Label = d.Label; s.Progress = d.Progress;
                s.Left = d.TicksLeft >= 0 && d.Busy ? $"{(d.TicksLeft + Game.Hz - 1) / Game.Hz} s" : d.Phase == "firing" ? (h.FireDownS(i) is int fs && fs >= 0 ? $"{fs} s" : "?") : ""; // firing: until the target is down at the present odds, "?" while the line is broken (Glyph fight r1 #6)
                s.Under = op.Down ? "" : d.Queued.Length > 0 ? d.Queued : d.Busy && d.OnDone.Length > 0 ? "→ " + d.OnDone
                    : s.Armed && op.Gun.Drawn ? $"drawn · {HandoverHost.StanceName(op.Gun.Stance)}" : "";
                s.CanCancel = d.CanCancel && !ended; s.CancelTip = $"Cancel {d.CancelVerb} — {d.CancelText}";
                s.Role = Op.Name((OpId)i); s.Hurt = op.Gun.Health < op.Gun.Max && !op.Down; s.CancelTitle = $"Cancel {d.CancelVerb}"; s.CancelBody = d.CancelText;
                s.InCar = op.InCar && !ended; s.LeftFate = "";
                if (!ended && !op.InCar && h.GoCardOpen) foreach (var (who, fate, _) in goLeft) if (who == s.Role) s.LeftFate = fate;
                s.Icon = AtlasIcon(op.Busy?.Do ?? (d.Phase is "walking" or "running" ? "move" : d.Verb), op.Busy?.Approach ?? "", d.Phase);
                s.StateTag = op.Down ? "DOWN" : op.CarryBody.Length > 0 && d.Phase != "walking" && d.Phase != "acting" ? "CARRYING" : op.CarryBody.Length > 0 ? "CARRYING" : d.Phase == "running" ? "RUNNING" : d.Busy ? "BUSY" : s.Hurt ? "HURT" : "READY";
                s.Pace = op.CarryBody.Length > 0 ? (i == (int)OpId.Enforcer ? $"walk only · {g.R.CellSpeed * Game.Hz / 1000f:0.0} m/s" : $"slow · {g.R.CarryUntrainedSpeed * Game.Hz / 1000f:0.0} m/s") : "";
                if (op.CarryBody.Length > 0) s.Icon = "s_carrying";
                s.Queued = d.Queued.StartsWith("then: ") ? "then " + d.Queued[6..] : d.Queued;
                // state, never a control (Fable C2, C4; the controls are taught once, by the hints)
                s.HintLine = op.Down ? "" : s.Hurt ? $"Health {op.Gun.Health} of {op.Gun.Max}" : s.Armed && s.Drawn ? $"Drawn · {HandoverHost.StanceName(op.Gun.Stance)}" : "";
                // under the agent: number and verb; over a busy one: the ring
                var t = h.Views?.Find("op/" + Op.Name((OpId)i)); bool on = t && t.gameObject.activeSelf;
                var m = Marks[i]; m.World = on ? t.position : null; m.Num = Nums[i]; m.Selected = h.Selected[i];
                m.Verb = op.Busy != null ? HandoverHost.Glyph(op.Busy.Do, op.Busy.Approach) : op.Walk != null ? (op.Running ? "»" : "›") : "✓";
                var r = Rings[i]; bool busyRing = on && d.Busy && d.Phase != "down" && !ended;
                // the beat after an act: it was acting (not walking to it) and the engine's end tick has come, so it ran out, not cancelled
                bool acting = busyRing && !(d.Phase is "walking" or "running") && op.Busy != null;
                if (r.WasActing && !acting && r.LastEnd >= 0 && g.Tick >= r.LastEnd && !op.Down) r.DoneAt = Time.unscaledTime;
                if (acting) r.LastEnd = op.BusyEnd; else if (!busyRing) r.LastEnd = -1;
                r.WasActing = acting;
                r.Done = !busyRing && on && !ended && !op.Down && Time.unscaledTime - r.DoneAt < DoneSeconds;
                r.Show = busyRing || r.Done;
                if (r.Done) { r.World = t.position; r.Selected = h.Selected[i]; r.Progress = 1000; r.CanCancel = false; r.ActAt = null; r.Glyph = "✓"; }
                else if (r.Show)
                {
                    bool walking = d.Phase is "walking" or "running";
                    r.World = t.position; r.Selected = h.Selected[i]; r.Progress = d.Progress; r.CanCancel = d.CanCancel; r.Tip = s.CancelTip;
                    r.Glyph = walking ? (d.QueuedGlyph.Length > 0 ? d.QueuedGlyph : d.Phase == "running" ? "»" : "›") : d.Glyph;
                    r.ActAt = walking && d.QueuedGlyph.Length > 0 ? d.ActAt : null; r.ActGlyph = d.QueuedGlyph;
                }
            }
            for (int k = 0; k < 4; k++) { var t = h.Views?.Find(Roles[k]); Cast[k].World = t && t.gameObject.activeSelf ? t.position : null; }
            // the hotbar
            var slots = h.PlateOf(h.Agent); // brief 013: carrying swaps knobs 1 and 2 to Put down and Hide, the rest dim
            for (int i = 0; i < 4; i++) { VerbRefused[i] = i < slots.Length && slots[i].Dim && slots[i].Id == "fire" && h.Agent == OpId.Enforcer && g.Vale == ValeAt.WithEnforcer; VerbHas[i] = i < slots.Length && !slots[i].Dim && !h.KnobTethered(h.Agent, slots[i]); VerbGlyph[i] = i < slots.Length ? slots[i].Glyph : "·"; VerbKey[i] = h.VerbKey(i); VerbIcon[i] = i < slots.Length ? (slots[i].Id == "act" && slots[i].Glyph == "car" ? "car" : AtlasIcon(slots[i].Id, slots[i].Id, "")) : ""; /* the end steps: out with Vale, the car */ VerbName[i] = i < slots.Length ? slots[i].Label : ""; }
            Agent = (int)h.Agent; AllStrips = h.ShowAllStrips; IntelOpen = h.IntelOpen; Fight = g.FightOn; CancelKey = h.controls.Label(PlayAction.Cancel);
            // strips (Glyph #8): busy, hurt and queued agents a full strip; a ready or downed one a 34 px line ("SCOUT · READY"), shown when selected or
            // down; ` shows every strip full
            for (int i = 0; i < 4; i++)
            {
                var s2 = Slots[i]; bool full = s2.Busy && !s2.Down || s2.Hurt || s2.Queued.Length > 0;
                s2.Show = AllStrips || full || s2.Down || s2.Selected; s2.Compact = !AllStrips && !full;
            }
            VerbLit = h.LitSlot; VerbCaption = $"{Names[(int)h.Agent]}: {(h.LitSlot >= 0 && h.LitSlot < slots.Length ? slots[h.LitSlot].Label : "")}";
            // the fight
            Fighters.Clear(); Down.Clear();
            foreach (var (id, gun) in h.Fighters()) { var w = h.WorldOf(id); if (w.HasValue) Fighters.Add(new Pips { World = w.Value, Hp = gun.Health, Max = Mathf.Max(gun.Max, gun.Health), Cell = id is "scout" or "fixer" or "tech" or "enforcer" }); }
            for (int i = 0; i < 4; i++) if (g.Ops[i].Down && Marks[i].World.HasValue) Down.Add(new Downed { World = Marks[i].World.Value, Line = $"{Names[i]} · down" }); // a state, not a control (Fable B1)
            // the cursor, the hover ring and the chip
            var hov = h.Hover; bool act = hov != null && hov.Kind != "crowd" && hov.Kind != "op" && !ended;
            Cursor = h.AimLine != null ? (h.AimNoLine ? 3 : 2) : act ? 1 : 0;
            HoverRing = act ? h.TargetWorld(hov) : null;
            string alt = !ended && h.Panel == null && h.Loud == null ? h.AimLine ?? (hov == null ? h.MoveChip : null) : null;
            var it = h.HoverIntent;
            if (alt != null) { ChipText = alt; Chip = h.AimLine == null ? ChipKind.Move : h.AimLine.StartsWith("CIVILIAN") || h.AimLine.StartsWith("Vale") ? ChipKind.AimWarn : ChipKind.Aim; }
            else if (it != null && hov != null && !ended && h.Panel == null && it.Actionable)
            {
                // the price, only where a right click acts; a target this agent can't act on shows the hover ring alone (Fable A6, A7: no "can't
                // act … select the Fixer", no "right click: actions")
                Chip = ChipKind.Price;
                var o = it.Order; ChipIcon = AtlasIcon(o.Do, o.Approach, ""); ChipUsd = it.Usd; ChipTime = it.Time; ChipTrail = it.Trail.Replace("T ", ""); ChipAlert = it.Alert.Replace("A ", "");
                ChipMark = MarkOf(it.Outcome); ChipSeen = it.Seen;
                if (it.Lead.Length > 0) ChipIcon = it.Lead; if (it.SeenMark.Length > 0) { ChipMark = it.SeenMark; ChipSeen = ""; } // the takedown chip: the way leads, the seen answer ends it (Glyph §1)
                ChipText = $"{it.Label}   {it.Usd}   ·   {it.Time}   ·   {it.Trail}   ·   {it.Alert}{(it.Outcome.Length > 0 ? $"   ·   {it.Outcome}" : "")}";
            }
            else Chip = ChipKind.None;
            Pops = h.Pops; Pulses = h.Pulses; Unknowns = h.Unknowns;
            Street(h);
            // the action panel
            PanelOpen = h.Panel != null;
            if (PanelOpen && cardsFor != h.Panel)
            {
                var pn = h.Panel; cardsFor = pn; PanelCards.Clear();
                PanelTitle = pn.T.Label; PanelAbout = pn.About; PanelNote = pn.Note; PanelAt = pn.Gui;
                PanelBefore.Clear(); PanelBefore.AddRange(pn.Before);
                int first = System.Array.IndexOf(h.Selected, true); PanelRole = first >= 0 ? Op.Name((OpId)first) : "scout"; PanelTarget = pn.T.Label;
                // the header: who the target is, and what the cell knows of them (as Intel: need, temperament, the secret once known)
                PanelPerson = pn.T.Kind == "person"; PanelBadge = Badge(pn.T); PanelSub = pn.T.Label; PanelNeed = PanelNeedMark = PanelTemp = ""; PanelSecret = false;
                if (PanelPerson)
                {
                    int ri = System.Array.IndexOf(Roles, pn.T.Key); var p = h.CastOf(pn.T.Key); // a cast person only (PersonByRole throws on anything else)
                    if (ri >= 0) { var row = Intel[ri]; PanelTarget = row.Name; PanelNeed = row.NeedIcon; PanelNeedMark = row.NeedMark; PanelTemp = row.TempIcon; PanelSub = row.Job; }
                    if (p != null && p.KNeed) PanelSub += " · " + NeedShort(p.Need); if (p != null && p.KTemperament) PanelSub += " · " + char.ToUpperInvariant(p.Temperament[0]) + p.Temperament[1..];
                    PanelSecret = p != null && p.Knows("secret");
                }
                string hintVerb = null; if (h.HintNow != null && h.Text != null) { var hi = h.Text.Hints2.Find(x => x.Id == h.HintNow.Id); hintVerb = hi?.Verb; }
                foreach (var row in pn.Rows)
                {
                    var i2 = row.I;
                    PanelCards.Add(new PanelCard
                    {
                        Key = h.AgentKeyLabel((int)row.By), Title = $"{Names[(int)row.By]} · {row.Verb}", Does = i2.Does, Available = row.Available,
                        Price = i2.Move ? i2.Time : $"{(i2.Walk.Length > 0 ? i2.Walk + " " : "")}{i2.Usd} · {i2.Time} · {i2.Trail} · {i2.Alert}{(i2.Outcome.Length > 0 ? $" · {i2.Outcome}" : "")}",
                        Lock = row.Available ? "" : $"Locked: {row.Reason}{(row.Unlock.Length > 0 ? $"\n{row.Unlock}" : "")}",
                        Verb = row.Key >= 0 ? h.VerbKey(row.Key) : "",
                        Icon = AtlasIcon(row.I.Order.Do, row.I.Order.Approach, ""), Usd = i2.Usd, Time = i2.Time, Trail = i2.Trail.Replace("T ", ""), Alert = i2.Alert.Replace("A ", ""),
                        Outcome = OutcomeWords(row.I, pn.T.Key), OutcomeMark = i2.Order?.Do == "turn" && i2.Orders > 1 && i2.Outcome == "turned" ? "half" : MarkOf(i2.Outcome),
                        Walk = i2.WalkS > 0 ? $"walk {i2.WalkS} s" : "", Reason = row.Available ? "" : row.Reason, Unlock = row.Available ? "" : row.Unlock, Role = Op.Name(row.By),
                        Name = row.Verb, Leverage = i2.Leverage, Suggested = hintVerb != null && hintVerb != "turn" && i2.Order != null && i2.Order.Do == hintVerb,
                    });
                    PanelCards[PanelCards.Count - 1].Extra.AddRange(i2.Rows);
                    if (i2.Lead.Length > 0) PanelCards[PanelCards.Count - 1].Icon = i2.Lead;
                }
            }
            if (!PanelOpen) cardsFor = null;
            for (int i = 0; i < PanelCards.Count && PanelOpen && i < h.Panel.Rows.Count; i++) PanelCards[i].Mine = h.Selected[(int)h.Panel.Rows[i].By];
            // the going-loud card
            LoudOpen = h.Loud != null; LoudLines.Clear();
            if (LoudOpen) { LoudCard(h); LoudProgress = h.GateProgress; LoudAt = h.Loud.Gui; }
            // the hint and the toast, the briefing
            Hint = h.HintNow != null && !ended && !h.ShowBriefing ? h.HintNow.Line : null; HintRing = h.HintNow?.Ring;
            Toast = h.Toast != null && Time.unscaledTime < h.ToastUntil ? h.Toast : null;
            BriefingOpen = h.ShowBriefing && !ended; if (BriefingOpen && Briefing.Count == 0) BriefingParts(h); else if (!BriefingOpen) Briefing.Clear();
            // the feed and the ticker
            Feed = h.Feed; Unread = h.Unread; FeedFold = g.FightOn; HistoryOpen = h.FeedHistoryOpen; Filter = h.FeedFilter;
            HistoryRows(h); // Sky 2026-10-18: the panel always shows the newest entry (collapsed), so the list is kept either way (cached by key)
            Ticker = null; for (int i = h.Talk.Count - 1; i >= 0; i--) if (h.Talk[i].Agent >= 0) { Ticker = h.Talk[i]; break; }
            TickerAge = Ticker == null ? 99f : Time.unscaledTime - Ticker.Born;
        }

        /// <summary>A history card passes the filter: learned, turned (an approach or call that went the cell's way), city, warnings (warning, fight).</summary>
        static bool Passes(string filter, HandoverHost.FeedCard c) => filter switch
        {
            "learned" => c.Kind == "learned", "turned" => (c.Kind is "approach" or "call") && c.Tone == HandoverHost.Feedback.Ok, "city" => c.Kind == "city",
            "warnings" => c.Kind is "warning" or "fight", _ => true,
        };

        /// <summary>The history: what happened, newest first, unread (still in the stack) then read; a card with no lines and no result is no row
        /// (Fable rule 10). The cell's chatter is not history (the Cell filter is gone).</summary>
        void HistoryRows(HandoverHost h)
        {
            string key = $"{Filter}:{h.Feed.Count}:{h.FeedHistory.Count}:{h.Unread}";
            if (key == historyKey) return; historyKey = key; History.Clear();
            static bool Says(HandoverHost.FeedCard c) => c.Footer.Length > 0 || c.Lines.Count > 0;
            for (int i = h.Feed.Count - 1; i >= 0; i--) { var c = h.Feed[i]; if (!c.Live && Says(c) && Passes(Filter, c)) History.Add(($"{HandoverHost.MissionClock(c.Started)}  {c.Title}{(c.Footer.Length > 0 ? " — " + c.Footer : "")}", c, !c.Read)); }
            for (int i = h.FeedHistory.Count - 1; i >= 0 && History.Count < 40; i--) { var c = h.FeedHistory[i]; if (Says(c) && Passes(Filter, c)) History.Add(($"{HandoverHost.MissionClock(c.Started)}  {c.Title}{(c.Footer.Length > 0 ? " — " + c.Footer : "")}", c, false)); }
        }

        void LoudCard(HandoverHost h)
        {
            var gate = h.Loud; var c = gate.Card;
            LoudLines.Add($"<b>GOING LOUD</b>  ·  {gate.What}");
            if (gate.Takedown) // brief 013: a takedown a known watcher sees (Glyph §1): the same card, its own numbers
            {
                LoudLines.Add($"Seen: Alert → {c.AlertTo}");
                if (c.PoliceInS >= 0) LoudLines.Add($"Who comes: police in ~{c.PoliceInS} s");
                LoudLines.Add("The body can't be hidden from who saw it.");
                string s0 = gate.Source switch { "right" => "the right button", _ => "the button" };
                LoudLines.Add($"Hold {s0} to commit · release or Esc to back off · the clock runs");
                return;
            }
            LoudLines.Add($"Now: Alert → {c.AlertTo}{(c.Civilians > 0 ? $" ({c.Civilians} civilian{(c.Civilians > 1 ? "s" : "")} see it)" : "")}");
            var who = new List<string>();
            if (c.PoliceInS >= 0) who.Add($"police in {c.PoliceInS} s (reported: {c.ReportedBy})");
            if (c.Seal >= 0) who.Add($"district sealed at {HandoverHost.MissionClock(c.Seal)}");
            if (c.RivalsInS >= 0) who.Add(c.RivalsInS == 0 ? "the rivals come now" : $"the rivals in {c.RivalsInS} s");
            if (c.PatrolBreaks) who.Add("the patrol breaks to you");
            if (who.Count > 0) LoudLines.Add("Who comes: " + string.Join(" · ", who));
            var lose = new List<string>();
            if (c.FightAfter == "loud") lose.Add($"the final fight turns loud ({c.Officers} officers + {c.ResponseOfficers} response)");
            if (c.GuardAllyLost) lose.Add("the guard won't help");
            if (c.WarningLost) lose.Add("Inez's warning");
            if (lose.Count > 0) LoudLines.Add("You lose: " + string.Join(" · ", lose));
            LoudLines.Add($"Costs: +{c.TrailPerShooting} T for the shooting, +{c.TrailPerHit} T per person hit, +{c.TrailPerDown} T more per person down");
            if (c.DownedCaptured && h.Text != null && h.Text.LoudCaptured.Length > 0) LoudLines.Add(h.Text.LoudCaptured);
            string src = gate.Source switch { "F" => h.controls.Label(PlayAction.Draw), "right" => "the right button", "both" => "both buttons", _ => "the button" };
            LoudLines.Add($"Hold {src} to commit · release or Esc to back off · the clock runs");
        }

        void BriefingParts(HandoverHost h)
        {
            var t = h.Text; if (t == null) return;
            BriefTitle = t.Title; BriefSub = $"Harbour District · {h.MissionStartsAt[..5]}"; BriefSituation = string.Join(" ", t.Card);
            BriefObjective = t.Objective; BriefWin = t.Win; BriefClock = t.ClockLine; BriefPurse = t.PurseLine;
            BriefFails.Clear(); foreach (var f in t.Fails) if (f.code is not ("engine-error" or "withdraw")) BriefFails.Add(f.line.Split(new[] { ". " }, System.StringSplitOptions.None)[0].TrimEnd('.'));
            BriefWays.Clear(); foreach (var hnt in t.Hints) { int cut = hnt.IndexOf(':'); BriefWays.Add(cut > 0 ? (hnt[..cut], hnt[(cut + 1)..].Trim()) : ("A way in", hnt)); }
            BeginKey = h.controls.Label(PlayAction.Pause);
            Briefing.Add((t.Title.ToUpperInvariant(), Part.Title));
            Briefing.Add((string.Join(" ", t.Card), Part.Body));
            Briefing.Add(("<b>Objective</b>   " + t.Objective, Part.Body));
            Briefing.Add(("<b>Win</b>   " + t.Win, Part.Body));
            Briefing.Add(("<b>Fail if</b>   " + string.Join("  ·  ", t.Fails.FindAll(f => f.code is not ("engine-error" or "withdraw")).ConvertAll(f => f.line.Split(new[] { ". " }, System.StringSplitOptions.None)[0].TrimEnd('.'))), Part.Body));
            Briefing.Add(("<b>Clock</b>   " + t.ClockLine, Part.Body));
            Briefing.Add(("<b>Purse</b>   " + t.PurseLine, Part.Body));
            Briefing.Add(("<b>Two ways in</b>", Part.Body));
            foreach (var hnt in t.Hints) Briefing.Add(("   ▸ " + hnt, Part.Body));
            Briefing.Add(("<b>The cell</b>", Part.Body));
            foreach (var k in new[] { "scout", "fixer", "tech", "enforcer" }) if (t.Cell.TryGetValue(k, out var cl)) Briefing.Add(("   " + cl, Part.Small));
            var c = h.controls;
            Briefing.Add(($"{c.Label(PlayAction.Pause)}: start the clock   ·   {c.Label(PlayAction.Briefing)}: this briefing   ·   Esc: close   ·   right click a person or thing for actions   ·   text: {t.Source}", Part.Footer));
        }

        /// <summary>The end card in the player's words (Fable §1 I): a headline, the result line from the text's fails, the tally, then the debrief
        /// ids as sentences. The replay verdict and hash are engineering: F12 only (they are in the session's .end.txt as well). The sentences
        /// for the debrief ids are host placeholders until Fable's text carries them; an id with no sentence is not shown.</summary>
        void EndCard(HandoverHost h)
        {
            RestartKey = h.controls.Label(PlayAction.Restart);
            var g = h.Game; var d = h.EndDebrief; Ended = true; EndLines.Clear();
            string failLine = h.Text?.Fails.Find(f => f.code == g.FailCode).line ?? "";
            string head = failLine.Length > 0 ? failLine.Split(new[] { ". " }, System.StringSplitOptions.None)[0].TrimEnd('.') : "";
            EndLines.Add(g.Result == Outcome.Success ? "Vale is in the car" : g.Result == Outcome.Withdrawn ? "You withdrew" : head.Length > 0 ? head : "The attempt failed");
            EndLines.Add(g.Result == Outcome.Success ? "The crew escapes with Vale alive." : failLine.Length > 0 ? failLine : g.FailText);
            EndLines.Add($"Done at {HandoverHost.MissionClock(g.Tick)}   ·   ${g.S.Purse - g.Cash:N0} spent   ·   Trail {g.Trail}");
            if (d != null)
            {
                string sec = d.PublicSecurity switch { "calm" => "Security is calm.", "raised" => "Security is raised.", "high" => "Security is on high alert.", _ => "" };
                string inv = d.InvestigationState switch { "little" => "Investigators: none yet.", "early" => "Investigators have a few leads.", "active" => "An investigation is open" + (Cause(d.InvestigationCause) is string cz ? $": {cz}." : "."), _ => "" };
                EndLines.Add($"{sec} {inv}".Trim());
                foreach (var st in h.StandingLines()) EndLines.Add(st.line); // brief 014: one Standing line per district (the debrief)
                var route = d.Route.ConvertAll(id => h.Text != null && h.Text.Route.TryGetValue(id, out var rw) ? rw : RouteWords(id)).FindAll(x => x != null); if (route.Count > 0) EndLines.Add(string.Join(" ", route));
                if (d.FightBranch.Length > 0) EndLines.Add($"Final fight: {(d.FightBranch == "loud" ? "the police" : "the rivals")}, {d.FightHostiles} against you, {d.FightHits} hit{(d.FightHits == 1 ? "" : "s")} taken.");
                var kept = d.Assets.ConvertAll(AssetWords).FindAll(x => x != null); if (kept.Count > 0) EndLines.Add("Kept: " + string.Join(" ", kept));
                Getaway(h, d);
            }
            if (h.ShowDebug) EndLines.Add(h.Ending);
        }

        /// <summary>Brief 018 §3 (Fable's debrief.between and debrief.getaway): who was left behind and their fate, each captive (talks in n, or
        /// holding out when the projection has them silent through the rescue window), and who saw the car leave.</summary>
        void Getaway(HandoverHost h, Debrief d)
        {
            var t = h.Text; if (t == null) return;
            static string Missions(int n) => n == 1 ? "1 mission" : $"{n} missions";
            string Who(string op) { var n = op.Length > 0 ? char.ToUpperInvariant(op[0]) + op[1..] : op; return "The " + n; }
            string W(System.Collections.Generic.Dictionary<string, string> m, string k) => m.TryGetValue(k, out var v) ? v : k;
            // Glyph r2: one row each: a captive's cameo and the cuffs, then Fable's captive line (holding out when they never talk in the window);
            // one walked out, its cameo and footprints, then the left-behind line. "\u0001op:fate\u0001" leads a row (the end card draws the cameo and glyph)
            t.DebriefBetween.TryGetValue("left-behind", out var lb);
            foreach (var (op, fate, why) in d.LeftBehind)
            {
                var c = d.Next.Captives.Find(x => x.Op == op);
                string line = null;
                if (c != null) { var cl0 = c.TalksAt == 0 ? t.GetawayHoldsOut : t.DebriefBetween.TryGetValue("captive", out var cl) ? cl : null; line = cl0?.Replace("{who}", Who(op)).Replace("{held-at}", t.GetawayPost) /* the engine names a post by its id: Fable's default */.Replace("{holds}", Missions(c.Holds)).Replace("{rescue-until}", Missions(c.RescueUntil)); }
                line ??= lb?.Replace("{who}", Who(op)).Replace("{fate}", W(t.GetawayFate, fate)).Replace("{why}", W(t.GetawayWhy, why));
                if (line != null) EndLines.Add($"\u0001{op}:{fate}\u0001{line}");
            }
            if (d.Next.CarSeenBy.Length > 0 && t.DebriefBetween.TryGetValue("car-seen", out var cs))
                EndLines.Add(cs.Replace("{by}", W(t.GetawayBy, PlayText.RoleOfId(d.Next.CarSeenBy))).Replace("{trail}", h.Game.R.WitnessTrail.ToString()));
        }

        static string RouteWords(string id) => id switch
        {
            "waitress-pulls-guard-away" => "Inez drew the guard off the door.", "guard-shot-beside-waitress" => "The guard was shot beside Inez.", "guard-shot-away-from-waitress" => "The guard was shot away from Inez.",
            "entrance-camera-looped" => "The entrance camera was looped.", "patrol-diverted" => "The patrol was rerouted.", "entry-timed-between-sweeps" => "The Enforcer went in between the patrol's sweeps.",
            "vale-taken-through-office" => "The Enforcer took Vale through the Office.", "vale-reaches-getaway-car" => "Vale reached the getaway car.",
            "driver-moves-vale-to-service-lane" => "The driver brought Vale round to the service lane.", "pickup-camera-blocked" => "The driver's car blinded the pickup camera.",
            "rivals-driven-back" => "The rivals were driven back.", "rivals-follow-false-tip" => "The rivals followed a false tip.", "vale-intercepted-outside" => "Vale was taken outside.",
            _ => null,
        };

        static string AssetWords(string id) => id switch
        {
            "waitress-introduces-cafe" => "Inez, who can introduce you at the café.", "waitress-may-be-questioned" => "Inez, though investigators may question her.",
            "waitress-may-identify-fixer" => "Inez, who could identify the Fixer.", "waitress-witnessed-shooting" => "Inez, who saw the shooting.", "driver-alters-pickups" => "The driver, who can alter pickups.",
            _ => null,
        };

        static string Cause(string id) => id switch
        {
            "waitress-shot" => "Inez was shot", "driver-shot" => "the driver was shot", "waitress-witnessed-shooting" => "Inez saw the shooting", "waitress-can-identify-fixer" => "Inez can identify the Fixer",
            "staged-distraction" => "the guard's trip to the café looks staged", "several-interventions" => "several traces lead to the cell", "few-leads" => "a few traces",
            _ => null,
        };

        static string NeedShort(string need) => need switch { "debt" => "in debt", "cash" => "short of cash", "" => "need unknown", _ => need.Replace('-', ' ') };

        /// <summary>A card's outcome in words (Glyph's frame 05): "Works", "Works, one step worse", "They would refuse", "?"; what a Watch or Listen
        /// learns; the other verbs' own outcome.</summary>
        static string OutcomeWords(HandoverHost.Intent i, string target)
        {
            var o = i.Order;
            if (o == null) return i.Outcome;
            switch (o.Do)
            {
                case "turn":
                    return i.Outcome switch
                    {
                        "turned" => i.Orders > 1 ? "Works, one step worse" : "Works", "kept" => "Works", "refused" => "They would refuse", "not-yet" => "Not yet: one more try",
                        _ => "? until the Scout learns who they are",
                    };
                case "watch": return i.Outcome == "its sight" ? "Learns what it sees" : "Learns their routine";
                case "listen": return "Learns their need and temperament";
                case "loop": return "Loops the camera; the Tech stays at the cabinet";
                case "call": return i.Outcome == "ready" ? "They act on the call" : "Not now";
                case "go": return i.Outcome == "win" ? "Wins the mission" : "Withdraws"; // brief 018: who stays is the Go line itself
                case "board": return i.Outcome.Length > 0 ? "In the car " + i.Outcome : "In the car";
                case "fire": return "Goes loud";
                default: return i.Outcome;
            }
        }

        static string Badge(HandoverHost.Target t) => t.Kind switch
        {
            "person" => t.Key switch { "waitress" => "badges/npc_waitress", "driver" => "badges/npc_driver", "councillor" => "badges/npc_councillor", "guard" => "badges/signal_guard", _ => "badges/npc_passer" },
            "rivals" => "badges/signal_rival", "patrol" => "badges/signal_unit", "security" or "hostile" => "badges/signal_police", "camera" => "badges/signal_camera", _ => "",
        };

        /// <summary>The kit's outcome mark for a card's outcome: turned or kept ok, refused no, not yet half, unknown "?".</summary>
        static string MarkOf(string outcome) => outcome.StartsWith("turned") || outcome.StartsWith("kept") || outcome == "win" ? "ok" : outcome.StartsWith("refused") ? "no" : outcome.StartsWith("not-yet") ? "half" : "unk";

        /// <summary>The kit's atlas icon for a verb (or an approach, or a phase), Glyph's names (icons_atlas: ask pay help pressure call watch listen …).</summary>
        public static string AtlasIcon(string verb, string approach, string phase) => verb switch
        {
            "turn" => approach is "ask" or "pay" or "help" or "pressure" ? approach : "ask",
            "watch" or "listen" or "mislead" or "loop" or "reroute" or "enter" or "take" or "fire" or "call" or "ask" or "pay" or "help" or "pressure" => verb,
            "act" => "enter", "board" or "go" => "car", "carry" => "s_carrying", "move" => "s_moving", "draw" or "holster" => "fire",
            "takedown" or "drop" or "stash" or "restrain" => verb, // Glyph's 013 glyphs
            "turn-view" => "turn_view", // brief 016's turn hint (H13): Glyph's turn glyph (briefs/014/glyph/glyphs/turn_view)
            _ => phase == "holding" ? "s_looping" : "s_ready",
        };

        /// <summary>The next mark on the timeline as its two parts for the pill: the label ("Rival car arrives", "?") and its time.</summary>
        static (string label, string at) NextParts(HandoverHost h)
        {
            var g = h.Game; var tl = h.Text?.Timeline;
            if (tl == null || tl.Count == 0) return ("Next", HandoverHost.MissionClock(h.NextMark()));
            foreach (var (at, label, known) in tl)
            {
                var p = at.Split(':'); int tick = (int.Parse(p[0]) * 60 + int.Parse(p[1])) * Game.Hz;
                if (tick > g.Tick) return (known ? label : "?", at);
            }
            return ("The close", h.Text.Close);
        }

        /// <summary>Two icons at most over a cast person: what the cell knows of them, and their state toward the cell.</summary>
        static (string a, string b) Icons(Game g, string role)
        {
            var p = g.PersonByRole(role);
            if (role == "councillor") return ("★", null);
            if (role == "guard") return (g.GuardAtDoor ? "◆door" : "◆", p.Turned ? "✓" : null);
            string known = p.KTemperament ? p.Temperament[..1].ToUpperInvariant() : "?";
            string need = p.Turned ? "✓" : p.KNeed ? "$" : p.Suspected.Length > 0 ? "$?" : null;
            return (known, need);
        }

        /// <summary>The briefing's timeline (Fable's): the next mark after now; a mark the cell can't know yet shows "?" until it has happened.</summary>
        static string NextOnTimeline(HandoverHost h)
        {
            var g = h.Game; var tl = h.Text?.Timeline;
            if (tl == null || tl.Count == 0) return "▶▶ " + HandoverHost.MissionClock(h.NextMark());
            foreach (var (at, label, known) in tl)
            {
                var p = at.Split(':'); int tick = (int.Parse(p[0]) * 60 + int.Parse(p[1])) * Game.Hz;
                if (tick > g.Tick) return $"▸ {at} {(known ? label : "?")}";
            }
            return "▸ the close";
        }
    }
}
