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
                if (n.StartsWith("crowd/") || n.StartsWith("act/")) continue;
                var t = TargetOf(x); if (t == null) continue;
                float up = t.Kind is "op" or "person" or "hostile" or "rivals" or "security" or "patrol" or "unit" ? 1.0f : t.Kind == "car" ? 0.8f : t.Kind is "body" or "stash" ? 0.25f : 0f;
                pickables.Add(new Pickable { T = t, Anchor = x.position + Vector3.up * up, Name = OneWord(t) });
            }
            return pickables;
        }

        /// <summary>A target's one-word name for the hover chip.</summary>
        public string OneWord(Target t) => t.Kind switch
        {
            "op" => char.ToUpperInvariant(t.Key[0]) + t.Key[1..],
            "person" => FirstWord(Name(t.Key)),
            "camera" => "Camera", "cabinet" => "Cabinet", "door" => "Door", "car" => "Car", "patrol" => "Patrol", "rivals" => "Rivals",
            "security" => "Police", "unit" => t.Label.Contains("robot") ? "Robot" : "Officer", "hostile" => t.Label.Contains("rival") ? "Rival" : "Officer",
            "body" => "Body", "stash" => "Stash", _ => "",
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

        /// <summary>Set by PointerHover: the hovered thing's anchor and name for the HUD.</summary>
        void HoverFeedback(Target t)
        {
            if (t == null || t.Kind is "crowd") { HoverAnchor = null; HoverName = ""; return; }
            foreach (var p in Pickables()) if (p.T.Kind == t.Kind && p.T.Key == t.Key) { HoverAnchor = p.Anchor; HoverName = p.Name; return; }
            var w = TargetWorld(t); HoverAnchor = w; HoverName = w.HasValue ? OneWord(t) : "";
        }

        // ---------------------------------------------------------------- the overlap stack (Sky, route B: Vale and his driver stand together at the car)

        Vector2 stackAt = new(-1e6f, -1e6f); int stackIndex; readonly List<Target> stack = new();
        /// <summary>How many things overlap under the cursor now, and which one a right click hits (0 = the best).</summary>
        public int HoverStackCount => stack.Count; public int HoverStackIndex => stack.Count == 0 ? 0 : stackIndex % stack.Count;

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

        /// <summary>Tab or the wheel over overlapping things: the next one is what a right click hits (its name on the chip).</summary>
        void StepStack(bool back = false)
        {
            if (stack.Count < 2) return; stackIndex = (stackIndex + (back ? stack.Count - 1 : 1)) % stack.Count; intentKey = ""; PointerHover(stackAt);
            Status = $"{OneWord(stack[stackIndex])} ({stackIndex + 1} of {stack.Count})";
        }

        // ---------------------------------------------------------------- target mode

        static readonly HashSet<string> TargetVerbs = new() { "watch", "listen", "mislead", "ask", "pay", "help", "pressure", "loop", "reroute", "takedown" };
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
