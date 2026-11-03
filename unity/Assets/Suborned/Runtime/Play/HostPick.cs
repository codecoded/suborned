using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Picking (Sky, playing, 2026-10-18: "finding it a little hard to select areas, like a camera"). View only: what a click hits and
    /// what the HUD marks; the orders are the same orders.
    /// Generous screen-space picking: every interactable (people, the cell, cameras, the cabinet, the door, cars, stashes, bodies, units and
    /// hostiles) has a pick circle round its screen anchor; when the cursor's ray hits nothing (or only a passer-by), the nearest anchor within
    /// 28 px at 1080p wins over the ground, a near tie going to the one the selected agent's verbs act on.
    /// Hover feedback: the hovered thing's anchor and a one-word name (HoverAnchor, HoverName) for the HUD's rim and name chip.
    /// Target mode: while a verb that needs a target is picked (Watch, Listen, a false tip, the Fixer's four ways, Loop, Reroute, Takedown),
    /// every valid target (the verb applies to it: the same Verb the panel prices) is listed for the HUD's markers and edge pins; Tab focuses
    /// them nearest the agent first (the view pans to one off screen, and its price shows); a marker's click gives the order as a right click on
    /// the target would. Esc cancels the pick, as before.</summary>
    public sealed partial class HandoverHost
    {
        public const float PickPx = 28f; // at 1080p

        public sealed class Pickable { public Target T; public Vector3 Anchor; public string Name = ""; }
        readonly List<Pickable> pickables = new(); float pickablesAt = -1f; int pickablesTick = -1;

        /// <summary>The hovered thing's screen anchor (world) and its one-word name, else null / "".</summary>
        public Vector3? HoverAnchor { get; private set; }
        public string HoverName { get; private set; } = "";

        /// <summary>Every interactable drawn now, with its anchor (refreshed at most every 0.1 s or on a tick).</summary>
        public IReadOnlyList<Pickable> Pickables()
        {
            if (viewRoot == null || Game == null) { pickables.Clear(); return pickables; }
            if (pickablesTick == Game.Tick && Time.unscaledTime - pickablesAt < 0.1f) return pickables;
            pickablesTick = Game.Tick; pickablesAt = Time.unscaledTime; pickables.Clear();
            foreach (var (n, x) in views.All())
            {
                if (!x || !x.gameObject.activeInHierarchy) continue;
                if (n.StartsWith("act/") || n.StartsWith("crowd/") && !CrowdSecurityView(n)) continue; // Pulse 067 W2: the crowd's shift guards are pickables ("Guard"); passers-by never
                var t = TargetOf(x); if (t == null) continue;
                float up = t.Kind is "op" or "person" or "bystander" or "hostile" or "rivals" or "security" or "patrol" or "unit" || t.Security ? 1.0f : t.Kind == "car" ? 0.8f : t.Kind is "body" or "stash" ? 0.25f : 0f;
                pickables.Add(new Pickable { T = t, Anchor = x.position + Vector3.up * up, Name = OneWord(t) });
            }
            return pickables;
        }

        /// <summary>Pulse 067 W2: a crowd view whose walker is security (Person.Civilian false), from its key alone (no target built).</summary>
        bool CrowdSecurityView(string n)
        {
            if (Game == null || n.Length <= 6) return false; int ci = 0;
            for (int i = 6; i < n.Length; i++) { char c = n[i]; if (c < '0' || c > '9') return false; ci = ci * 10 + (c - '0'); } // "crowd/<n>", parsed without a string a view
            return ci < Game.Walkers.Count && !Game.Walkers[ci].P.Civilian;
        }

        /// <summary>A target's one-word name for the hover chip.</summary>
        public string OneWord(Target t) => t.Kind switch
        {
            "crowd" when t.Security => FirstWord(t.Label.StartsWith("an ") ? t.Label[3..] : t.Label.StartsWith("a ") ? t.Label[2..] : t.Label), // Pulse 067 W2: "Guard"
            "op" => char.ToUpperInvariant(t.Key[0]) + t.Key[1..],
            "person" => FirstWord(Name(t.Key)),
            "camera" => "Camera", "cabinet" => "Cabinet", "door" => "Door", "car" => "Car", "patrol" => "Patrol", "rivals" => "Rivals",
            "security" => "Police", "unit" => t.Label.Contains("robot") ? "Robot" : "Officer", "hostile" => t.Label.Contains("rival") ? "Rival" : "Officer",
            "body" => "Body", "stash" => "Stash", "bystander" => "Bystander", _ => "",
        };
        static string FirstWord(string s) { s = s.StartsWith("the ") ? s[4..] : s; int i = s.IndexOf(' '); s = i > 0 ? s[..i] : s; return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s[1..] : s; }

        /// <summary>Whether any of the agent's verbs acts on this kind of target (a cheap test, for near ties).</summary>
        static bool Acts(OpId by, Target t) => by switch
        {
            OpId.Scout => t.Kind is "person" or "camera" or "patrol" or "rivals",
            OpId.Fixer => t.Kind == "person" && t.Key != "councillor",
            OpId.Tech => t.Kind is "camera" or "cabinet" or "patrol",
            _ => t.Kind is "door" or "car" or "person" or "hostile" or "body" or "stash",
        };

        /// <summary>The nearest pickable within the pick radius of a screen point, a near tie (6 px) going to one the agent acts on.</summary>
        Target NearestPick(Vector2 screen, bool agents)
        {
            if (!cam) return null; float k = cam.pixelHeight / 1080f, r = PickPx * k, tie = 6f * k;
            Pickable near = null, nearActs = null; float dn = float.MaxValue, da = float.MaxValue;
            foreach (var p in Pickables())
            {
                if (p.T.Kind == "op" && !agents && !Game.Ops[(int)Op.Parse(p.T.Key)].Down) continue; // a standing agent is a left-click target
                var sp = cam.WorldToScreenPoint(p.Anchor); if (sp.z <= 0) continue;
                float d = ((Vector2)sp - screen).magnitude; if (d > r) continue;
                if (d < dn) { dn = d; near = p; }
                if ((agents ? p.T.Kind == "op" : Acts(Agent, p.T)) && d < da) { da = d; nearActs = p; }
            }
            return nearActs != null && da <= dn + tie ? nearActs.T : near?.T;
        }

        /// <summary>Brief 085: the door, the cabinet and a stash are generous boxes, and the door guard stands in the door's. A ray into the box found the box, so the 28 px circle every other
        /// person has (NearestPick, only when the ray finds nothing) never ran for him: his exact body was the target, and a pixel beside it was the door, which for most agents has no way and so no
        /// pill. Here a being within the same circle counts as under the ray, and the rows then decide between them as they do on his body. A click on the box away from him stays the door's.</summary>
        void PickBesideMarker(Vector2 screen, List<(Target t, float score)> found, bool agents)
        {
            besideNow = null; if (agents || !cam || !found.Exists(x => x.t.Kind is "door" or "cabinet" or "stash") || !found.TrueForAll(x => x.t.Kind is "door" or "cabinet" or "stash" || Passer(x.t))) return;
            float best = PickPx * cam.pixelHeight / 1080f; Pickable near = null;
            foreach (var p in Pickables())
            {
                if (!(p.T.Kind is "person" or "hostile" or "patrol" or "rivals" or "security" or "unit" || p.T.Security)) continue;
                var sp = cam.WorldToScreenPoint(p.Anchor); if (sp.z <= 0f) continue; float d = ((Vector2)sp - screen).magnitude; if (d <= best) { best = d; near = p; }
            }
            if (near != null) { besideNow = near.T; found.Add((near.T, PickRank(near.T, false) * 10000f)); BesidePicks++; }
        }
        Target besideNow;
        /// <summary>The rows' rank where things overlap under the pointer. A person whose body the ray hit, standing in a marker's box, is him for anyone with a row on him, even only a harsh one: the Enforcer's pointer on the guard is the guard (his takedown is in the ring), and the box round him stays the door's. Beside his body the rows decide.</summary>
        int RankOver(Target t, List<(Target t, float score)> found)
        {
            int r = RowRank(t);
            return r == 1 && t.Kind == "person" && !ReferenceEquals(t, besideNow) && found.Exists(x => x.t.Kind is "door" or "cabinet" or "stash") ? 0 : r;
        }
        /// <summary>The proof reads it: picks a marker's box would have taken from the person standing in it.</summary>
        public int BesidePicks { get; private set; }

        /// <summary>Set by PointerHover: the hovered thing's anchor and name for the HUD.</summary>
        void HoverFeedback(Target t)
        {
            if (t == null || Passer(t)) { HoverAnchor = null; HoverName = ""; return; } // Pulse 067 W2: a shift guard is named on hover ("Guard"); a passer-by never
            foreach (var p in Pickables()) if (p.T.Kind == t.Kind && p.T.Key == t.Key) { HoverAnchor = p.Anchor; HoverName = p.Name; return; }
            var w = TargetWorld(t); HoverAnchor = w; HoverName = w.HasValue ? OneWord(t) : "";
        }

        // ---------------------------------------------------------------- the overlap stack (Sky, route B: Vale and his driver stand together at the car)

        Vector2 stackAt = new(-1e6f, -1e6f); int stackIndex; readonly List<Target> stack = new();
        /// <summary>How many things overlap under the cursor now, and which one a right click hits (0 = the best).</summary>
        public int HoverStackCount => stack.Count; public int HoverStackIndex => stack.Count == 0 ? 0 : stackIndex % stack.Count;
        /// <summary>Brief 067 F9: the overlap stack under the cursor (best first), for the stack tab and the others' rings.</summary>
        public IReadOnlyList<Target> HoverStack => stack;

        /// <summary>Brief 067 F8 (Glyph): a candidate under the cursor ranked for hover and the right button: first whether the selection has an
        /// open calm way on it (0), else any row (1), else none (2), then its kind (a being 0, a body or a downed agent 1, a camera 2, the car or a
        /// stand 3, the door marker, the cabinet or a stash 4, a passer-by 9). The rows are the selection's own (ContextFor), asked once a tick
        /// for each candidate (a crowded spot has two to four). Pulse 067 W2: a shift guard in the crowd is a being (0), ranked by its rows.</summary>
        float PickRank(Target t, bool rows = true)
        {
            int kind = t.Kind switch
            {
                "person" or "hostile" or "patrol" or "rivals" or "security" or "unit" or "bystander" => 0, "body" => 1, "op" => 1, "camera" => 2, "car" or "stand" => 3,
                "door" or "cabinet" or "stash" => 4, "crowd" => t.Security ? 0 : 9, _ => 5,
            };
            if (Passer(t)) return 3 * 10 + kind; // a passer-by: never before anything with a row
            return (rows ? RowRank(t) * 10 : 0) + kind;
        }
        /// <summary>0: the selection has an open calm way on t; 1: a row (locked, or only violent); 2: none. Cached a tick, a selection and an order count.</summary>
        int RowRank(Target t)
        {
            if (Game == null) return 2; int mask = 0; for (int k = 0; k < 4; k++) if (Selected[k]) mask |= 1 << k;
            var key = $"{t.Kind}/{t.Key}/{t.Id}"; var stamp = (Game.Tick / Game.Hz, mask, Game.Orders.Count); // once a second
            if (rowRanks.TryGetValue(key, out var c) && c.stamp.Equals(stamp)) return c.rank;
            int rank = 2; ContextPlan plan = null;
            if (Guard("pick rank", () => plan = ContextFor(t, lastHover)) && plan?.Pn != null && plan.Pn.Rows.Count > 0)
                rank = plan.Choice.Order.Exists(w => plan.Row(w)?.I?.Order is Order o && !ContextRank.Violent(new ContextRank.Way { Do = o.Do })) ? 0 : 1;
            if (rowRanks.Count > 64) rowRanks.Clear();
            rowRanks[key] = (stamp, rank); return rank;
        }
        readonly Dictionary<string, ((int, int, int) stamp, int rank)> rowRanks = new();

        /// <summary>How much the selected agent wants this target in a tie: Vale at his car for the Enforcer's Take most, then anything the
        /// agent's verbs act on.</summary>
        float Prefer(Target t)
        {
            if (Game == null) return 0f;
            if (Agent == OpId.Enforcer && Game.Vale == ValeAt.Pickup && t.Kind == "person" && t.Key == "councillor") return 50f;
            return Acts(Agent, t) ? 5f : 0f;
        }

        /// <summary>The stack under the cursor, best first; the same cursor keeps the step Tab or the wheel chose.</summary>
        Target Stacked(Vector2 screen, List<Target> list, bool agents)
        {
            if (agents) return list.Count > 0 ? list[0] : null; // a left click selects: no stepping
            bool same = (screen - stackAt).sqrMagnitude < 16f && list.Count == stack.Count && list.TrueForAll(x => stack.Exists(y => y.Kind == x.Kind && y.Key == x.Key));
            if (!same) { stackIndex = 0; stack.Clear(); stack.AddRange(list); stackAt = screen; }
            return stack.Count == 0 ? null : stack[stackIndex % stack.Count];
        }

        /// <summary>Steps through an overlap stack this run (the wheel, Z, Tab): H22's gone-when (Fable 067).</summary>
        public int StackSteps { get; private set; }
        /// <summary>Tab or the wheel over overlapping things: the next one is what a right click hits (its name on the chip).</summary>
        void StepStack(bool back = false)
        {
            if (stack.Count < 2) return; stackIndex = (stackIndex + (back ? stack.Count - 1 : 1)) % stack.Count; intentKey = ""; StackSteps++; PointerHover(stackAt);
            Status = $"{OneWord(stack[stackIndex])} ({stackIndex + 1} of {stack.Count})";
        }

        // ---------------------------------------------------------------- target mode

        static readonly HashSet<string> TargetVerbs = new() { "watch", "listen", "mislead", "ask", "pay", "help", "pressure", "loop", "disable", "reroute", "takedown" };
        readonly List<Pickable> targets = new(); string targetsKey = ""; float targetsAt = -10f; int focusIndex = -1;

        /// <summary>A verb that needs a target is picked.</summary>
        public bool TargetMode { get { if (Picked < 0 || Game == null || Modal != ModalGate.Modal.None) return false; var p = PlateOf(Agent); return Picked < p.Length && !p[Picked].Dim && TargetVerbs.Contains(p[Picked].Id); } }

        /// <summary>The valid targets of the picked verb (the panel's own Verb applies to them), refreshed on a new pick and every second.</summary>
        public IReadOnlyList<Pickable> TargetMarks
        {
            get
            {
                if (!TargetMode) { targets.Clear(); targetsKey = ""; focusIndex = -1; return targets; }
                var key = $"{Agent}:{Picked}:{Way}";
                if (key == targetsKey && Time.unscaledTime - targetsAt < 1f) return targets;
                if (key != targetsKey) focusIndex = -1;
                targetsKey = key; targetsAt = Time.unscaledTime; targets.Clear();
                foreach (var p in Pickables())
                {
                    if (p.T.Kind is "op" or "unit" or "crowd") continue;
                    Intent i = null; var pt = p; Guard("targets", () => i = Verb(Agent, Picked, pt.T));
                    if (i?.Order == null || i.Move) continue;
                    Answer lk = default; lk.ok = true; Guard("targets", () => lk = Locked(Agent, i, pt.T));
                    if (!lk.ok && lk.code != "agent-busy") continue; // a refused target (Heard, Watched, away) gets no marker
                    targets.Add(p);
                }
                var from = frame.World(Game.Ops[(int)Agent].Pos);
                targets.Sort((a, b) => (a.Anchor - from).sqrMagnitude.CompareTo((b.Anchor - from).sqrMagnitude));
                return targets;
            }
        }

        /// <summary>The target Tab has focused, else null.</summary>
        public Pickable Focused => focusIndex >= 0 && focusIndex < targets.Count ? targets[focusIndex] : null;

        /// <summary>Tab in target mode: the next valid target, nearest the agent first; the view pans to it when off screen; its price shows.</summary>
        void FocusNext(bool back = false)
        {
            var list = TargetMarks; if (list.Count == 0 || !cam) return;
            focusIndex = back ? (focusIndex <= 0 ? list.Count - 1 : focusIndex - 1) : (focusIndex + 1) % list.Count; var f = list[focusIndex];
            var vp = cam.WorldToViewportPoint(f.Anchor); if (vp.z <= 0 || vp.x < 0.08f || vp.x > 0.92f || vp.y < 0.08f || vp.y > 0.92f) LookAt(f.Anchor);
            var sp = cam.WorldToScreenPoint(f.Anchor); intentKey = ""; PointerHover(sp);
            Status = $"{OneWord(f.T)} ({focusIndex + 1} of {list.Count})";
        }

        /// <summary>A target marker clicked: the picked verb on it, as a right click on the target gives it.</summary>
        public void TargetClick(Pickable p)
        {
            if (p == null || !cam || !TargetMode) return;
            var sp = cam.WorldToScreenPoint(p.Anchor); if (sp.z <= 0) { LookAt(p.Anchor); sp = cam.WorldToScreenPoint(p.Anchor); }
            Physics.SyncTransforms(); intentKey = ""; PointerHover(sp); PointerRight(sp);
        }

        /// <summary>An off-screen target's edge pin clicked: pan to it and focus it.</summary>
        public void TargetFind(Pickable p) { if (p == null) return; LookAt(p.Anchor); focusIndex = targets.IndexOf(p); if (cam) { intentKey = ""; PointerHover(cam.WorldToScreenPoint(p.Anchor)); } }
    }
}
