using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 049 round 2 (Glyph; Sky: "rings should be red when they're firing at you, and with crosshairs also when involved at being
    /// aimed or shooting"): one ground ring per target instead of the 15 m range circle. Combatants (the guard, the patrol, officers, units,
    /// rivals, the fight's hostiles) wear one while a selected agent has its weapon drawn or a fight is on; a civilian, a car or a camera only
    /// under the cursor when the pill offers fire. One state per target, priority fire > aim > ours > hover > in > long > out: shape says reach
    /// (solid, thin, dashed), colour says whose (guard and police amber, rivals violet, a civilian cream) or that it threatens the cell (red),
    /// four ticks say an aimed shot is in the exchange. The host reads the engine (FireWhyOf, UnderFireOf, FireStateOf, the weapons' rings and
    /// the site's sight) and the HUD draws them as ground decals. View only: nothing reaches the engine.</summary>
    public sealed partial class HandoverHost
    {
        public enum RingSide { Civilian, Guard, Rival }
        public enum RingReach { In, Long, Out }
        public sealed class TargetRing { public string Id = "", Key = ""; public Vector3 At; public RingSide Side; public RingReach Reach; public bool Hover, Ours, Aim, Fire; public float ShotAt = -10f; }
        /// <summary>This frame's target rings (HudToolkit.TargetRings draws them).</summary>
        public readonly List<TargetRing> TargetRings = new();
        /// <summary>The view keys wearing a target ring this frame (HostViews.RivalRings leaves those out).</summary>
        public readonly HashSet<string> TargetRingKeys = new();
        /// <summary>Who aims at the cell now (FireWhyOf: aiming) and who shot at it, with the tick of the last shot (UnderFireOf).</summary>
        readonly HashSet<string> threatAim = new(); readonly Dictionary<string, int> threatShot = new();
        readonly Dictionary<string, float> shotSeen = new(); readonly Dictionary<string, int> shotTick = new();
        readonly Dictionary<string, (int tick, RingReach reach)> reachCache = new();
        int threatTick = -1;
        readonly List<TargetRing> ringPool = new();

        /// <summary>The threat sets, once a tick: FireWhyOf for every agent (aiming, firing) and UnderFireOf's last shot per shooter.</summary>
        void ThreatsRead()
        {
            var g = Game; if (g == null || threatTick == g.Tick) return; threatTick = g.Tick;
            threatAim.Clear(); threatShot.Clear();
            for (int k = 0; k < 4; k++)
            {
                if (g.Ops[k].Down) continue;
                Guard("ring threats", () => { foreach (var w in g.FireWhyOf((OpId)k)) if (w.Aiming) threatAim.Add(w.By); });
                foreach (var u in UnderFireNow[k]) if (!threatShot.TryGetValue(u.By, out var t) || u.Last > t) threatShot[u.By] = u.Last;
            }
        }

        /// <summary>The best reach any of `shooters` has on `id` at `at` (sight on the site, the weapon's effective and max rings), once a tick.</summary>
        RingReach ReachOf(string id, Pt at, List<int> shooters)
        {
            var g = Game; if (reachCache.TryGetValue(id, out var c) && c.tick == g.Tick) return c.reach;
            var best = RingReach.Out;
            foreach (int k in shooters)
            {
                var op = g.Ops[k]; var w = op.Gun.W; if (w == null) continue;
                long dx = op.Pos.X - at.X, dy = op.Pos.Y - at.Y; long d2 = dx * dx + dy * dy;
                if (d2 > (long)w.MaxCm * w.MaxCm) continue;
                bool line = false; try { line = g.S.Sight(op.Pos, at, g.R.Eye); } catch (System.Exception) { line = false; }
                if (!line) continue;
                var r = d2 <= (long)w.EffCm * w.EffCm ? RingReach.In : RingReach.Long; if (r < best) best = r;
                if (best == RingReach.In) break;
            }
            reachCache[id] = (g.Tick, best); return best;
        }

        /// <summary>This frame's rings (after the views moved).</summary>
        void TargetRingsFrame()
        {
            TargetRings.Clear(); TargetRingKeys.Clear(); var g = Game; if (g == null || views == null || Game.Over) return;
            ThreatsRead();
            var shooters = new List<int>(); for (int k = 0; k < 4; k++) if ((Selected[k] || Driving && k == dAgent) && Armed(k) && Drawn(k)) shooters.Add(k);
            bool combat = shooters.Count > 0 || g.FightOn;
            if (shooters.Count == 0 && g.FightOn) for (int k = 0; k < 4; k++) if (Armed(k)) shooters.Add(k); // a fight on and nobody selected drawn: the cell's reach
            string hoverId = ""; if (Hover != null && (HoverFire || Aiming)) { hoverId = FireIdOf(Hover, out _); }
            float now = Time.unscaledTime; int used = 0;
            void Add(string id, RingSide side, bool combatant)
            {
                if (string.IsNullOrEmpty(id)) return;
                bool aim = threatAim.Contains(id), fire = threatShot.TryGetValue(id, out var last) && g.Tick - last <= Game.Hz;
                bool hover = id == hoverId; bool ours = false; for (int k = 0; k < 4; k++) if (FireNow[k].Target == id && !string.IsNullOrEmpty(FireNow[k].Mode)) ours = true;
                if (!(aim || fire || ours || hover || combatant && combat)) return;
                var key = ViewKey(id); var tr = views.Find(key); if (!tr || !tr.gameObject.activeInHierarchy) return;
                // GunOf only for a combatant: on a person of the crowd it MAKES a gun the first time it is named (engine state: a hover must never
                // change the run); PosOf is read-only and gives null for anyone down or off the street
                if (combatant) { var gun = g.GunOf(id); if (gun != null && gun.Down) return; }
                var at = g.PosOf(id); if (!at.HasValue) return;
                if (threatShot.TryGetValue(id, out var lt) && (!shotTick.TryGetValue(id, out var seen) || seen != lt)) { shotTick[id] = lt; shotSeen[id] = now; } // the kick, once a shot
                var r = used < ringPool.Count ? ringPool[used] : null; if (r == null) { r = new TargetRing(); ringPool.Add(r); } used++;
                r.Id = id; r.Key = key; r.At = tr.position; r.Side = side; r.Hover = hover; r.Ours = ours; r.Aim = aim; r.Fire = fire;
                r.ShotAt = shotSeen.TryGetValue(id, out var sa) ? sa : -10f;
                r.Reach = shooters.Count > 0 ? ReachOf(id, at.Value, shooters) : RingReach.Out;
                TargetRings.Add(r); TargetRingKeys.Add(key);
            }
            if (!g.Range)
            {
                if (!g.GuardAlly) Add("guard", RingSide.Guard, true);
                Add("patrol", RingSide.Guard, true);
                foreach (var sg in g.SecGuns) Add(sg.Id, RingSide.Guard, true);
                Add("rivals", RingSide.Rival, true);
                foreach (var u in g.Units) if (!u.Legacy && !u.Patrol && u.OnStreet(g.Tick)) Add(u.Id, RingSide.Guard, true);
            }
            foreach (var h in g.Hostiles) if (h.Present) Add(h.Id, h.Id.StartsWith("rival") ? RingSide.Rival : RingSide.Guard, true);
            if (hoverId.Length > 0 && !TargetRingKeys.Contains(ViewKey(hoverId))) Add(hoverId, RingSide.Civilian, false); // a civilian, a car or a camera: under the cursor only
        }
    }
}
