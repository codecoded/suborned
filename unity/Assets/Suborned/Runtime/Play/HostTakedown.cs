using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 013 E in the play host (Latch; Glyph's spec briefs/013/glyph-takedown-hud.md; Fable's words in text.json): the takedown on the
    /// Enforcer's plate (knock out by default, the knob again flips to kill), its card from the engine's QuoteTakedown, the body rows (carry, put
    /// down, hide, tie) for any agent, the plate swap while carrying, the Tech's loop on any camera the cabinet reaches (entrance, pickup, the
    /// square's mast), and what the HUD draws in the street: body markers, stash plates, held doors, a report in transit, the response's units
    /// and its pill. Everything here reads the engine; orders still go in as engine orders, so a session replays exactly.</summary>
    public sealed partial class HandoverHost
    {
        /// <summary>The armed takedown's way: knockout (the default every time it is armed) or kill (the knob pressed again).</summary>
        public string Way { get; private set; } = "knockout";

        // ---------------------------------------------------------------- names

        public string PersonName(Person p) => p.Role.Length > 0 ? Name(p.Role) : Text?.TargetName("crowd", "a passer-by") ?? "a passer-by";
        public string StashName(StashPlace st) => Text?.TargetName(st.Kind == "door" ? "stash-door" : "stash-bin", st.Kind == "door" ? "the service door" : "the bin") ?? st.Id;
        public string CameraName(string id) => id == "entrance" || id == "pickup" ? Text?.TargetName("camera-" + id, $"the {id} camera") ?? $"the {id} camera"
            : Text != null && Text.TargetNames.TryGetValue("camera-" + id, out var n) ? n : "the square's mast camera";
        static bool IsOpName(string s) => s is "scout" or "fixer" or "tech" or "enforcer";
        static string KeyOf(Person p) => HostViews.KeyOf(p);

        // ---------------------------------------------------------------- the plate

        static readonly Slot DropSlot = new("drop", "Put down", "drop"), StashSlot = new("stash", "Hide", "stash");
        /// <summary>An agent's knobs: their verbs, or while carrying a body Put down and Hide on 1 and 2, the rest dimmed (Glyph §4).</summary>
        public Slot[] PlateOf(OpId by)
        {
            var own = Slots[(int)by];
            if (by == OpId.Enforcer && Game != null && Game.Vale == ValeAt.WithEnforcer) // the end steps: out with Vale, the first knob is the car
            { var v = (Slot[])own.Clone(); v[0] = new Slot("act", Text?.Actions.TryGetValue("extract", out var ex) == true && ex.Label.Length > 0 ? ex.Label : "Vale into the car", "car"); return v; }
            if (Game == null || Game.Ops[(int)by].CarryBody.Length == 0) return own;
            var p = new Slot[Math.Max(2, own.Length)]; p[0] = DropSlot; p[1] = StashSlot;
            for (int i = 2; i < own.Length; i++) p[i] = new Slot(own[i].Id, own[i].Label, own[i].Glyph, true);
            return p;
        }

        /// <summary>A carry knob pressed: Put down here, or Hide in the nearest stash with room (walking there first).</summary>
        void PlateAct(string id)
        {
            var op = Game.Ops[(int)Agent];
            if (id == "drop") { var o = new Order { Op = Op.Name(Agent), Do = "drop" }; if (Issue(o, null, null)) SayOrder(Agent, o, null); return; }
            var st = NearestStash(op.Pos);
            if (st == null) { PopKey("no-room", null, SelectedWorld(), Feedback.Refused, "No hiding place with room."); return; }
            var t = new Target { Kind = "stash", Key = st.Id, Id = st.Id, Label = StashName(st) };
            Give(Agent, new Order { Op = Op.Name(Agent), Do = "stash", Target = st.Id }, t, null);
        }

        StashPlace NearestStash(Pt from)
        {
            StashPlace best = null; long bd = long.MaxValue;
            foreach (var st in Game.S.Stashes) { if (StashFree(st.Id) <= 0) continue; long d = G.D2(st.Stand, from); if (d < bd) { bd = d; best = st; } }
            return best;
        }

        public int StashUsed(string id) { int n = 0; foreach (var b in Game.Bodies) if (b.Stash == id) n++; return n; }
        public int StashFree(string id) { var st = Game.S.Stashes.Find(x => x.Id == id); return st == null ? 0 : Game.HoldsOf(st) - StashUsed(id); }

        // ---------------------------------------------------------------- the Tech's loop (Glyph §9)

        bool Loopable(string cam) { var c = Game.S.Cameras.Find(x => x.Id == cam); return c != null && (Game.R.LoopTargets.Contains(cam) || (c.World && Game.R.LoopTargets.Contains("world"))); }

        Intent LoopIntent(int slot, string cam)
        {
            var R = Game.R;
            var i = new Intent { Slot = slot, Label = $"Loop {CameraName(cam)}", Order = new Order { Op = "tech", Do = "loop", Target = cam } };
            i.Time = $"{R.LoopS} s"; i.Trail = $"T +{R.LoopTrail}"; i.Outcome = "tethered";
            var k = Game.Known("camera:" + cam);
            i.Rows.Add(("watch", k != null && k.Sight ? $"Blinds: what {CameraName(cam)} sees{(Game.S.Cameras.Find(x => x.Id == cam)?.Facings.Length > 1 ? " (all three heads)" : "")}" : "Blinds: ? until the Scout watches it"));
            if (Game.LoopOn && Game.LoopCam != cam) i.Rows.Add(("loop", $"Replaces the loop on {CameraName(Game.LoopCam)}"));
            return i;
        }

        /// <summary>The cabinet's card lists every camera it reaches, one row each (Glyph §9): the rows after the first loop row.</summary>
        IEnumerable<Intent> MoreLoops(int slot, Target t)
        {
            if (t.Kind != "cabinet") yield break;
            foreach (var c in Game.S.Cameras) if (c.Id != "entrance" && Loopable(c.Id)) yield return LoopIntent(slot, c.Id);
        }

        // ---------------------------------------------------------------- the takedown (Glyph §1, §2)

        Person PersonOf(Target t) => t.Kind switch { "person" => Game.PersonByRole(t.Key), "crowd" or "body" => Game.PersonById(t.Id.Length > 0 ? t.Id : t.Key), _ => null };

        /// <summary>A takedown on a person, one way, priced by the engine (QuoteTakedown, read-only).</summary>
        Intent TakedownIntent(int slot, Target t, string how)
        {
            if (t.Kind is not ("person" or "crowd")) return null;
            var p = PersonOf(t); if (p == null || p.Down) return null;
            if (p == Game.ValeP && how == "knockout") return null; // Vale: the refusal pill only (Glyph §2)
            var q = Game.QuoteTakedown(KeyOf(p), how);
            // the victim is no watcher of his own takedown (the engine's ScanAct skips him; QuoteTakedown's SeenAt does not yet: reported to Keel)
            if (p == Game.GuardP) { q.Seen.SeenBy.Remove("guard"); q.Seen.MaybeSeenBy.Remove("guard"); q.Seen.Seen = q.Seen.SeenBy.Count > 0 ? "yes" : q.Seen.MaybeSeenBy.Count > 0 ? "?" : "no"; }
            var i = new Intent { Slot = slot, How = how, Td = q, Label = $"{(how == "kill" ? "Kill" : "Knock out")} {PersonName(p)}", Order = new Order { Op = "enforcer", Do = "takedown", Target = KeyOf(p), How = how } };
            i.Usd = "$0"; i.Time = $"{q.S} s"; i.Lead = how == "kill" ? "way_kill" : "way_knock";
            bool seen = q.Seen.Seen == "yes";
            i.Trail = $"T +{q.TrailIfFound}"; i.Alert = $"A {(seen ? q.AlertIfSeen : q.AlertIfFound)}";
            i.SeenMark = seen ? "no" : q.Seen.Seen == "?" || q.People.Exists(x => x.verb == "?") ? "unk" : "ok";
            i.Outcome = ""; i.Seen = SeenWords(q.Seen); // the way's rows say what follows (Glyph §2)
            // the way's own rows: found, never found, wakes or never talks
            string doors = q.HoldsDoors.Count == 0 ? "holds no door" : $"holds {string.Join(", ", q.HoldsDoors.ConvertAll(DoorName))} ~{Clock(q.HoldS)}";
            if (how == "kill")
            {
                i.Rows.Add(("door_held", $"Found: police in ~{q.PoliceInS} s · {doors} · searches 30 m · Menace"));
                i.Rows.Add(("p_unknown", $"Never found: missing, +{q.TrailIfMissing} T, the area hardens, the case stays open"));
                var ties = TiesNames(p);
                i.Rows.Add(("talker", $"Never talks{(q.Ties > 0 ? $" · {q.Ties} tie{(q.Ties == 1 ? "" : "s")}{(ties.Length > 0 ? ": " + ties : "")}" : "")}"));
            }
            else
            {
                i.Rows.Add(("door_held", $"Found: unit in ~{q.PoliceInS} s · {doors}"));
                i.Rows.Add(("p_unknown", $"Never found: comes home, +{q.TrailIfMissing} T, a statement"));
                i.Rows.Add(("nextevent", $"Wakes {(q.WakesAt >= 0 ? MissionClock(q.WakesAt) : "?")} · knows where and when, {(q.Aware == "yes" ? "and your face" : "not your face")}"));
            }
            // a reporter on his way: taking him down stops the report (Glyph §7)
            foreach (var r in Game.Reps) if (r.P == p && r.Kind != "tell" && r.Phase is "walk" or "call" or "react" or "check" && r.LandAt > Game.Tick && r.LandAt < int.MaxValue / 4)
                { i.Rows.Insert(0, ("call", $"Stops their report: it lands in {(r.LandAt - Game.Tick + Game.Hz - 1) / Game.Hz} s")); break; }
            return i;
        }

        string TiesNames(Person p)
        {
            var names = new List<string>();
            foreach (var role in new[] { "waitress", "driver", "guard", "councillor" }) { var c = Game.PersonByRole(role); if (c != p && p.Lover == c && c.Knows("secret")) names.Add(Name(role)); }
            return string.Join(", ", names);
        }

        string DoorName(string id) => id switch { "office-entrance" => "the Office door", "official-entrance" => "the official door", "office-service-door" => "the service door", "cafe-entrance" => "the café door", "cafe-kitchen-door" => "the kitchen door", _ => id };
        static string Clock(int s) => $"{s / 60}:{s % 60:00}";

        /// <summary>The card's words for a takedown way (text.json actions.takedown.ways) and the body verbs; false for any other verb.</summary>
        bool TakedownWords(Intent v, Target t)
        {
            var o = v.Order; if (Text == null) return false;
            switch (o.Do)
            {
                case "takedown":
                    {
                        var a = Text.Actions.TryGetValue("takedown", out var at) ? at : null; var p = PersonOf(t);
                        var (label, does) = a != null && a.Ways.TryGetValue(o.How, out var w) ? w : (o.How == "kill" ? "Kill" : "Knock out", "");
                        v.Name = $"{label} {(p != null ? PersonName(p) : t.Label)}"; v.Does = does; return true;
                    }
                case "carry" when !IsOpName(o.Target): case "restrain": case "stash": case "drop":
                    {
                        var (label, does) = Text.Action(o.Do, TextKey(t));
                        v.Name = o.Do switch { "stash" => $"Hide in {t.Label}", "drop" => label, _ => $"{label} {t.Label}" }; v.Does = does; return true;
                    }
                case "loop":
                    {
                        v.Name = $"Loop {CameraName(o.Target)}"; v.Does = Text.Action("loop", t.Kind == "camera" ? TextKey(t) : "cabinet").does;
                        if (o.Target != "entrance") v.Does = v.Does.Replace("the entrance camera", CameraName(o.Target)).Replace("The entrance camera", Cap(CameraName(o.Target))); // Fable's line names the entrance camera only
                        return true;
                    }
            }
            return false;
        }

        /// <summary>The body rows any agent has on a body (carry, tie, put down when it is theirs) or a stash (hide the body carried). Only = one verb.</summary>
        List<Intent> BodyIntents(OpId by, Target t, string only = null)
        {
            var l = new List<Intent>(); var g = Game; var R = g.R; var op = g.Ops[(int)by]; string who = Op.Name(by);
            Intent I(string verb, string target, string label, int s) { var i = new Intent { Slot = -1, Label = label, Order = new Order { Op = who, Do = verb, Target = target } }; i.Time = $"{s} s"; i.Usd = "$0"; i.Trail = "T +0"; i.Alert = "A ?"; return i; }
            bool carrying = op.CarryBody.Length > 0;
            if (t.Kind == "body")
            {
                var b = g.BodyById(t.Key); if (b == null || b.Seizing || !b.Open) return l;
                if (b.Carrier == by && (only == null || only == "drop")) { var d = I("drop", "", "Put down", R.DropS); d.Rows.Add(("drop", "Anyone who passes may find the body.")); l.Add(d); }
                if (!b.Carrier.HasValue && b.P.Down && !carrying && (only == null || only == "carry"))
                {
                    var c = I("carry", t.Key, $"Carry {t.Label}", R.CarryS);
                    c.Rows.Add(("s_carrying", by == OpId.Enforcer ? $"walk only · {R.CellSpeed * Game.Hz / 1000f:0.0} m/s" : $"slow · {R.CarryUntrainedSpeed * Game.Hz / 1000f:0.0} m/s"));
                    l.Add(c);
                }
                if (!b.Carrier.HasValue && !b.Dead && !b.Restrained && b.P.Down && !carrying && (only == null || only == "restrain")) l.Add(I("restrain", t.Key, $"Tie up {t.Label}", R.RestrainS));
            }
            if (t.Kind == "stash" && (only == null || only == "stash"))
            {
                var st = g.S.Stashes.Find(x => x.Id == t.Key);
                if (st != null && (carrying || only == null))
                {
                    var h = I("stash", st.Id, $"Hide in {StashName(st)}", st.Kind == "door" ? R.StashDoorS : R.StashContainerS);
                    h.Rows.Add(("stash", $"{StashName(st)}: {StashFree(st.Id)} free · a search at Alert 2 within 30 m opens it"));
                    if (st.Kind != "door") { var (rt, known) = g.NextRound(st.Id); h.Rows.Add(("nextevent", known && rt >= 0 ? $"Emptied at {MissionClock(rt)} (watched)" : "Emptied at ? (watch the round)")); }
                    if (carrying) l.Add(h);
                }
            }
            if (t.Kind == "stash" && only == "drop" && carrying) l.Add(I("drop", "", "Put down", R.DropS));
            return l;
        }

        /// <summary>The panel's key rows: a takedown card answers 1 (knock out) and 2 (kill); otherwise each row its plate slot.</summary>
        void KeyRows(ActionPanel pn)
        {
            bool td = pn.Rows.Exists(r => r.I?.Order?.Do == "takedown");
            if (td) // the takedown card: its two ways first, after "Before you act" (Glyph §2); the agent's other rows after them
            {
                var ways = pn.Rows.FindAll(r => r.I?.Order?.Do == "takedown"); pn.Rows.RemoveAll(r => r.I?.Order?.Do == "takedown"); pn.Rows.InsertRange(0, ways);
            }
            foreach (var r in pn.Rows) r.Key = td ? (r.I?.Order?.Do == "takedown" ? (r.I.How == "kill" ? 1 : 0) : -1) : r.Slot;
            if (pn.T.Kind == "cabinet")
            {
                var tech = pn.Rows.FindIndex(r => r.I?.Order?.Do == "loop");
                if (tech >= 0)
                {
                    var first = pn.Rows[tech]; int at = tech + 1;
                    foreach (var v in MoreLoops(first.Slot, pn.T))
                    {
                        Words(v, pn.T); var lk = Locked(first.By, v, pn.T);
                        var aw = ActPlace(first.By, v.Order, pn.T); if (aw != null) { v.Walk = first.I.Walk; v.WalkS = aw.Seconds; }
                        pn.Rows.Insert(at++, new PanelRow { By = first.By, Slot = first.Slot, Key = -1, I = v, Verb = v.Name, Lock = lk, Reason = lk.ok ? "" : lk.why, Available = lk.ok, Unlock = lk.ok ? "" : LockLine(v.Order, lk) ?? "" });
                    }
                }
            }
        }

        /// <summary>"Before you act" (Glyph §2): the same for both ways, said once: who sees it, whether he sees you coming, who is near, his
        /// next check-in (only once his routine is known), Alert if seen.</summary>
        void Before(ActionPanel pn)
        {
            var row = pn.Rows.Find(r => r.I?.Td != null); if (row == null) return;
            var q = row.I.Td; var p = PersonOf(pn.T);
            pn.Before.Add(("watch", q.Seen.Seen == "no" ? "Seen by: nobody known." + (q.Seen.MaybeSeenBy.Count > 0 ? "" : "") : q.Seen.Seen == "?" ? "Seen by: ? an unknown watcher could see it." : $"Seen by: {SeenWords(q.Seen)}."));
            pn.Before.Add(("p_unknown", q.Aware == "no" ? "From behind: they will not see you coming." : q.Aware == "yes" ? (q.Armed ? "They see you coming: a fight." : "They see you coming: a struggle, others may hear.") : "Whether they see you coming: ?"));
            if (q.People.Count > 0)
            {
                var ws = q.People.ConvertAll(x => { var wp = Game.PersonById(x.id); string nm = wp != null && wp.Role.Length > 0 ? Name(wp.Role) : "a passer-by"; return x.verb == "?" ? $"{nm}: ?" : x.landS >= 0 ? $"{nm}: {x.verb}, lands in {x.landS} s" : $"{nm}: {x.verb}"; });
                pn.Before.Add(("listen", $"{q.People.Count} {(q.People.Count == 1 ? "person" : "people")} within 13 m. {string.Join("; ", ws)}"));
            }
            else pn.Before.Add(("listen", "Nobody within 13 m."));
            if (q.NextMark >= 0) pn.Before.Add(("nextevent", $"Next check-in: {MissionClock(q.NextMark)}. A missed one sends a robot."));
            pn.Before.Add(("p_alert", $"If seen: Alert {q.AlertIfSeen} · police in ~{q.PoliceInS} s"));
        }

        /// <summary>A takedown a known watcher sees puts up the going-loud card (Glyph §1): the 0.5 s hold commits it, release backs off.</summary>
        bool SeenGate(Intent i, Target t, Vector2? screen, string source)
        {
            if (i?.Td == null || i.Td.Seen.Seen != "yes" || Loud != null) return false;
            var card = new LoudCard { Loud = true, Why = "takedown-seen", AlertTo = i.Td.AlertIfSeen, PoliceInS = i.Td.PoliceInS, ReportedBy = "seen" };
            Loud = new Gate { Card = card, Source = source, What = i.Label, Start = Time.unscaledTime, At = TargetWorld(t), Gui = screen.HasValue ? new Vector2(screen.Value.x, (cam ? cam.pixelHeight : Screen.height) - screen.Value.y) : Mouse, Takedown = true, Target = t, By = Agent };
            Loud.Orders.Add(i.Order);
            Status = $"Seen: hold {SourceName(source)} {HoldSeconds:0.0} s to commit, release to back off";
            return true;
        }

        // ---------------------------------------------------------------- what the street shows (Glyph §3, §5, §6, §7, §8)

        public sealed class BodyMark { public Vector3 World; public string Key = "", Name = ""; public bool Dead, Tied, Found, Pulse, Hidden; public float Wake; public string Chip = ""; }
        public sealed class StashMark { public Vector3 World; public string Id = "", Name = ""; public int Holds, Used; public bool Behind, Hover; }
        public sealed class DoorMark { public Vector3 World; public string Id = "", Text = ""; public bool Locked, Preview; public float Frac = 1f; }
        public sealed class ReportMark { public Vector3 World; public Vector3? To; public string Icon = "call"; public float Frac; public int LandsS; }
        public sealed class UnitBadge { public Vector3 World; public bool Robot; public int Count; }
        public sealed class EdgeChip { public Vector3 World; public string Text = ""; }
        public readonly List<BodyMark> BodyMarks = new(); public readonly List<StashMark> StashMarks = new(); public readonly List<DoorMark> DoorMarks = new();
        public readonly List<ReportMark> ReportMarks = new(); public readonly List<UnitBadge> UnitBadges = new(); public readonly List<EdgeChip> Arrivals = new();
        /// <summary>The response pill (Glyph §8): its line, and its rows when open (R or a click).</summary>
        public string ResponseLine { get; private set; } = ""; public readonly List<(string icon, string label, string text)> ResponseRows = new();
        public bool ResponseOpen { get; set; }
        public bool RespAny { get; private set; } public int RespOfficers { get; private set; } public int RespRobots { get; private set; } public int RespComing { get; private set; } public int RespSoonestS { get; private set; } = -1;

        /// <summary>Per frame (positions follow the eased views), cheap: the body, stash, door, report and unit marks.</summary>
        public void StreetMarks()
        {
            BodyMarks.Clear(); StashMarks.Clear(); DoorMarks.Clear(); ReportMarks.Clear(); UnitBadges.Clear(); Arrivals.Clear(); ResponseRows.Clear(); ResponseLine = "";
            if (Game == null || views == null) return;
            var g = Game; int tick = g.Tick; bool alt = Peeking;
            bool carrying = false; for (int k = 0; k < 4; k++) carrying |= g.Ops[k].CarryBody.Length > 0;
            // bodies: one marker each over the torso; hidden ones on Alt at their stash
            foreach (var b in g.Bodies)
            {
                if (b.Seizing || !b.P.Down) continue;
                var key = "body/" + KeyOf(b.P); var tr = views.Find(key);
                bool found = false; foreach (var r in g.Reps) if (r.Act == b.Act && r.Phase == "landed") found = true;
                var m = new BodyMark { Key = KeyOf(b.P), Name = PersonName(b.P), Dead = b.Dead, Tied = b.Restrained, Found = found, Hidden = !b.Open };
                if (!b.Dead && b.WakeAt < int.MaxValue / 4) { int total = Game.T(g.R.WakeS), left = Math.Max(0, b.WakeAt - tick); m.Wake = 1f - Mathf.Clamp01(left / (float)Math.Max(1, total)); m.Pulse = left <= 15 * Game.Hz; m.Chip = $"wakes {MissionClock(b.WakeAt)} · left {MissionClock(left)}"; }
                if (b.Open && tr && tr.gameObject.activeSelf) m.World = tr.position + Vector3.up * (b.Carrier.HasValue ? 0.5f : 0.45f);
                else if (!b.Open && alt) { var st = g.S.Stashes.Find(x => x.Id == b.Stash); if (st == null) continue; m.World = frame.World(st.At) + Vector3.up * 2.4f; }
                else continue;
                BodyMarks.Add(m);
            }
            // stashes: while anyone carries, on hover, with Alt (never idle)
            foreach (var st in g.S.Stashes)
            {
                bool hover = Hover != null && Hover.Kind == "stash" && Hover.Key == st.Id;
                if (!(carrying || hover || alt)) continue;
                var at = frame.World(st.At);
                var (used, holds) = g.StashRoom(st.Id);
                StashMarks.Add(new StashMark { World = at + Vector3.up * (st.Kind == "door" ? 2.6f : 1.6f), Id = st.Id, Name = st.Kind == "door" ? "Service door" : "Dumpster", Holds = holds, Used = used, Behind = !CellSees(st.Stand), Hover = hover });
            }
            // held doors: a lock while a unit holds the scene; at Alert 3 "locked"
            foreach (var e in g.S.Entrances)
            {
                if (!g.DoorHeld(e.id)) continue;
                int until = g.HeldUntil(e.id); bool never = until == Game.Never;
                if (!heldFrom.ContainsKey(e.id)) heldFrom[e.id] = tick;
                int span = Math.Max(1, until - heldFrom[e.id]), left = Math.Max(0, until - tick);
                DoorMarks.Add(new DoorMark { Id = e.id, World = frame.World(e.at) + Vector3.up * 2.8f, Locked = never, Text = never ? "locked" : $"held ~{MissionClock(left)}", Frac = never ? 1f : 1f - Mathf.Clamp01(left / (float)span) });
            }
            foreach (var id in new List<string>(heldFrom.Keys)) if (!g.DoorHeld(id)) heldFrom.Remove(id);
            // the takedown card's preview: a hollow lock on each door the body would hold (hover a way's card)
            if (Panel != null && PanelHover >= 0 && PanelHover < Panel.Rows.Count && Panel.Rows[PanelHover].I?.Td is TakedownQuote pq)
                foreach (var id in pq.HoldsDoors) { var e = g.S.Entrances.Find(x => x.id == id); if (e.id != null && !g.DoorHeld(id)) DoorMarks.Add(new DoorMark { Id = id, World = frame.World(e.at) + Vector3.up * 2.8f, Preview = true, Text = $"would hold ~{Clock(pq.HoldS)}" }); }
            // a report in transit: only while an agent sees the person
            foreach (var r in g.Reps)
            {
                if (r.P == null || r.Kind == "tell" || r.Phase is "stopped" or "landed" or "watcher" || r.LandAt <= tick || r.LandAt >= int.MaxValue / 4 && r.Phase != "check") continue;
                var pos = g.PosOfPerson(r.P); if (!pos.HasValue || !CellSees(pos.Value)) continue;
                var tr = views.Find(r.P.Role.Length > 0 ? r.P.Role : CrowdKey(r.P));
                var w = tr && tr.gameObject.activeSelf ? tr.position : frame.World(pos.Value);
                int left = r.LandAt < int.MaxValue / 4 ? r.LandAt - tick : -1;
                ReportMarks.Add(new ReportMark { World = w + Vector3.up * 2.3f, To = r.Phase is "walk" or "call" ? frame.World(r.Place) : null, Icon = r.Phase == "radio" ? "radio" : r.Phase is "check" or "react" ? "watch" : "call", LandsS = left < 0 ? -1 : (left + Game.Hz - 1) / Game.Hz, Frac = left < 0 ? 0f : 1f - Mathf.Clamp01(left / (30f * Game.Hz)) });
            }
            // the response's units: badges merged within 2.5 m (Alt splits them); the pill once anything is sent
            int onStreet = 0, robots = 0, officers = 0, coming = 0, soonest = int.MaxValue; bool any = false;
            foreach (var u in g.Units)
            {
                if (u.Legacy || u.Patrol || u.St == "gone") continue;
                any = true;
                var pose = g.UnitPose(u);
                if (pose.HasValue && u.OnStreet(tick))
                {
                    onStreet++; if (u.Robot) robots++; else officers++;
                    var tr = views.Find("unit/" + u.Id); var w = (tr && tr.gameObject.activeSelf ? tr.position : frame.World(pose.Value.pos)) + Vector3.up * (u.Robot ? 1.8f : 2.3f);
                    var near = alt ? null : UnitBadges.Find(x => x.Robot == u.Robot && (x.World - w).sqrMagnitude < 2.5f * 2.5f);
                    if (near != null) near.Count++; else UnitBadges.Add(new UnitBadge { World = w, Robot = u.Robot, Count = 1 });
                    if (cam) { var vp = cam.WorldToViewportPoint(w); if (vp.z > 0 && (vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1) && u.St == "walk") { var ec = Arrivals.Find(a => (a.World - w).sqrMagnitude < 400f); if (ec == null) Arrivals.Add(new EdgeChip { World = w, Text = u.Robot ? "1 robot" : "1 officer" }); } }
                }
                else { coming++; int at = Math.Max(u.Launch, u.Arrive); if (at > tick && at < soonest) soonest = at; }
            }
            RespAny = any; RespOfficers = officers; RespRobots = robots; RespComing = coming; RespSoonestS = soonest < int.MaxValue ? (soonest - tick + Game.Hz - 1) / Game.Hz : -1; // brief 014: the pill as icons and numbers
            if (any)
            {
                var lv = g.DialNow;
                ResponseLine = $"RESPONSE · {officers} officer{(officers == 1 ? "" : "s")} · {robots} robot{(robots == 1 ? "" : "s")} · {coming} coming";
                ResponseRows.Add(("units", "On the street", $"{officers} officers · {robots} robots"));
                ResponseRows.Add(("nextevent", "Coming", coming > 0 ? $"{coming}{(soonest < int.MaxValue ? $" · {(soonest - tick + Game.Hz - 1) / Game.Hz} s" : "")}" : "none"));
                ResponseRows.Add(("p_time", "More", lv.ReinforceS > 0 ? $"+1 unit every {lv.ReinforceS} s, up to {lv.UnitCap}" : "none"));
                ResponseRows.Add(("lock", "Seal", g.Lockdown < int.MaxValue / 4 ? $"sealed at {MissionClock(g.Lockdown)}" : $"{Clock(lv.SealS)} after the unit confirms"));
            }
        }

        /// <summary>The carry chip on ground hover (Glyph §4): the walk at the carrying pace, the destination kind, the doors a body left there
        /// would hold (DoorsInSightOf), and who sees the way (Game.CarryPathSight, the SeenQuote test second by second; "?" for unknown watchers).</summary>
        string CarryChip(int k, Pt to)
        {
            CarrySight c = null; lastCarry = null; if (!Guard("carry sight", () => c = Game.CarryPathSight((OpId)k, to)) || c == null) return null;
            lastCarry = c.Refused.Length > 0 ? null : c;
            if (c.Refused.Length > 0) return null;
            var st = Game.S.Stashes.Find(x => G.Within(x.Stand, to, Game.R.AtReachCm));
            string dest = st != null ? $"{(st.Kind == "door" ? "Service door" : "Dumpster")}: {StashFree(st.Id)} free" : "Open ground: found if seen" + (c.Seen == "?" ? " (?)" : "");
            var doors = Game.DoorsInSightOf(to); string held = doors.Count == 0 ? "out of sight of doors: holds none" : "holds: " + string.Join(", ", doors.ConvertAll(DoorName));
            var seen = new List<string>(); foreach (var kv in c.SeenS) seen.Add($"{WatcherName(kv.Key)}, {kv.Value} s"); if (c.MaybeS.Count > 0) seen.Add("?");
            return $"{AgentNames[k]} carrying: {c.S} s · {dest} · {held} · seen on the way: {(seen.Count == 0 ? "nobody known" : string.Join("; ", seen))}";
        }

        CarrySight lastCarry; // brief 014: the carry chip's parts
        readonly Dictionary<string, int> heldFrom = new(); // when each held door's hold was first seen (its ring's start)
        string CrowdKey(Person p) { for (int i = 0; i < Game.Walkers.Count; i++) if (Game.Walkers[i].P == p) return "crowd/" + i; return ""; }

        // ---------------------------------------------------------------- the feed's cards for the brief 013 codes (text.json cards.items)

        /// <summary>A card from Fable's `cards` for a log code (exact, or by prefix: stash.), with its sight rule (in-sight at the place `at`
        /// names, read from the game: the log carries no position) and `needs` (routine). Returns false when the text has no card for the code;
        /// hidden = the card exists but the cell can't see it (its spoken lines stay quiet too).</summary>
        bool TextCard(LogLine l, out bool hidden)
        {
            hidden = false; var c = Text?.CardFor(l.Code); if (c == null) return false;
            var who = WhoIn(l);
            Pt? at = c.At switch
            {
                "victim" or "person" or "reporter" => who != null ? (Game.BodyOf(who) is Body b && b.P.Down ? Game.BodyPos(b) : Game.PosOfPerson(who)) : null,
                "stash" => who != null && Game.BodyOf(who) is Body sb ? Game.S.Stashes.Find(x => x.Id == sb.Stash)?.At : null,
                "scene" => Game.Acts.Count > 0 ? Game.Acts[Game.Acts.Count - 1].Scene : (Pt?)null,
                "guard" => Game.GuardPose()?.pos, "patrol" => Game.PatrolPose()?.pos,
                "door" => Game.SceneHolds.Count > 0 ? Game.SceneHolds[Game.SceneHolds.Count - 1].At : (Pt?)null,
                "unit" => UnitNear(), _ => null,
            };
            if (c.Sight == "in-sight" && !(at.HasValue && CellSees(at.Value))) { hidden = true; return true; }
            if (c.Needs == "routine" && !(who?.KRoutine ?? false)) { hidden = true; return true; }
            string result = c.Result;
            if (c.Results.Count > 0) result = c.Results.TryGetValue(CardPick(l, who), out var r) ? r : new List<string>(c.Results.Values)[0];
            var card = NewCard(c.Kind, c.Title, c.Kind == "fight" ? "fire" : c.Kind == "warning" ? "!" : "·", who != null && (c.At.Length > 0 || c.Kind == "city") ? Cap(PersonName(who)) : "", /* the city's answer never says who reported (Fable) */ at.HasValue ? frame.World(at.Value) : null, who != null && who.Role.Length > 0 ? who.Role : "");
            card.Event = l.Code; card.Footer = Cap(FillCard(result, l, who)); card.Tone = c.Kind is "warning" or "fight" ? Feedback.Refused : Feedback.Info;
            lastEvent = card;
            CardBeat(c, l, who); // brief 014: the card's story beat, if it has one
            return true;
        }

        Pt? UnitNear() { foreach (var u in Game.Units) if (!u.Legacy && !u.Patrol && Game.UnitPose(u) is var p && p.HasValue) return p.Value.pos; return null; }

        /// <summary>The person a log line is about: a cast role in the text, else the first word that is a person's id (the engine writes Who(p)).</summary>
        Person WhoIn(LogLine l)
        {
            var role = RoleIn(l); if (role != null) return Game.PersonByRole(role);
            foreach (var w in l.Text.Split(' ', ',', '.', ':', '(', ')', '\'')) { if (w.Length < 3) continue; var p = w.EndsWith("s") && Game.PersonById(w) == null ? Game.PersonById(w[..^1]) : Game.PersonById(w); if (p != null) return p; }
            foreach (var r in new[] { "waitress", "driver", "guard", "councillor" }) if (l.Text.StartsWith(r, StringComparison.Ordinal)) return Game.PersonByRole(r);
            return null;
        }

        /// <summary>Which of a card's results applies (the card's `pick`, in the engine's terms).</summary>
        string CardPick(LogLine l, Person who)
        {
            string t = l.Text;
            switch (l.Code)
            {
                case "vale.exits": case "vale.at-car": return Game.PickupService ? "service" : "front";
                case "fight.ambush": return t.Contains("warning") ? "warned" : "sprung";
                case "takedown.starts": return t.Contains("sees it coming") ? "struggle" : "unseen";
                case "takedown.seen": return t.Contains("carried") ? "carry" : "act";
                case "carry.body": return t.StartsWith("The enforcer", StringComparison.OrdinalIgnoreCase) ? "enforcer" : "other";
                case "report.started": return t.Contains("radio") ? "radio" : t.Contains("look") || t.Contains("check") ? "look" : "phone";
                case "report.confirmed": return t.Contains("dead") ? "dead" : "out";
                case "alarm.dispatches": return t.Contains(" 0 unit") ? "robots" : "both";
                case "stash.opened": return t.Contains("inside") || t.Contains("finds") ? "found" : "empty";
            }
            if (l.Code.StartsWith("stash.")) return l.Code[6..];
            return "";
        }

        string FillCard(string s, LogLine l, Person who)
        {
            var m = System.Text.RegularExpressions.Regex.Match(l.Text, @"\d+");
            string agent = "the Scout"; foreach (var k in OpKeys) if (l.Text.Contains(Cap(k)) || l.Text.Contains(k + " ")) { agent = "the " + Cap(k); break; }
            int clockTick = l.Code == "seal.starts" || l.Code == "fight.loud" ? Game.Lockdown : l.Code == "alarm.dispatches" ? NextUnitTick() : Game.Tick;
            string whoName = who != null ? PersonName(who) : "someone";
            if (l.Code == "report.confirmed") { int i = l.Text.IndexOf(" confirms", StringComparison.Ordinal); whoName = i > 0 ? (l.Text.StartsWith("the camera", StringComparison.Ordinal) ? CameraName(l.Text[11..i]) : "the response") : "the response"; }
            if (l.Code == "takedown.seen" && l.Text.StartsWith("The ", StringComparison.Ordinal)) { int i = l.Text.IndexOf(" sees", StringComparison.Ordinal); var w = i > 4 ? l.Text[4..i] : ""; whoName = Game.S.Cameras.Exists(c => c.Id == w) ? CameraName(w) : w.Length > 0 ? Name(w) : "someone"; }
            return s.Replace("{who}", whoName).Replace("{agent}", agent).Replace("{alert}", Game.Alert.ToString())
                .Replace("{n}", m.Success ? m.Value : "?").Replace("{clock}", clockTick < int.MaxValue / 4 ? MissionClock(clockTick) : "?");
        }

        int NextUnitTick() { int best = int.MaxValue; foreach (var u in Game.Units) if (!u.Legacy && !u.Patrol && u.Launch > Game.Tick && u.Launch < best) best = u.Launch; return best; }

        /// <summary>The witness-reacts scenes are found by event, then outcome (Fable): the engine's verb ends its witness.react line.</summary>
        static string ReactOutcome(LogLine l)
        {
            var t = l.Text.TrimEnd('.', ' '); int i = t.LastIndexOf(':'); return i < 0 ? null : t[(i + 1)..].Trim();
        }
    }
}
