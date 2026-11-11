using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 059 K8 (engine 0.32.0/0.33.0; Sky on 0.2.122: "rivals just suddenly appeared when near car, they should be coming in from
    /// somewhere when the chime comes"): the rival crew drawn from the engine's one read, Game.RivalPoses(): five people (rival-1..rival-5, each
    /// its own view keyed by its engine id) and their car (car/rivals, Vela's Sedan_Rival: brief 067 L, the plum car with the ink bubble, so it never
    /// reads as Vale's teal-and-ivory sedan or the cell's coral one; the box when the kit is not copied). Nobody
    /// of the crew is drawn before the car turns in (the engine lists none); a member aboard rides unseen in the car; on foot he is drawn where
    /// the engine has him, facing as it says; in the final fight the same view keeps drawing him (the engine's fight body starts where he stood,
    /// M059-B1), so nobody vanishes and nobody new appears at the ambush spots (059 §2 point 1); down, he lies where he fell, or his body view
    /// draws him (taken down, carried, put down). The crew's hostile views in the fight's list are left empty in the Handover (the range still
    /// draws its spawned rivals there). View only: nothing goes back to the engine.</summary>
    public sealed partial class HostViews
    {
        public static readonly string[] RivalKeys = { "rival-1", "rival-2", "rival-3", "rival-4", "rival-5" };
        /// <summary>A crew member's engine id (rival-1..rival-5): the Handover's crew, never the range's spawned rivals.</summary>
        public static bool IsCrewId(string id) => System.Array.IndexOf(RivalKeys, id) >= 0;
        /// <summary>Each member's mode now ("aboard", "walking", "posted", "talking", "boarding", "fight", "down"; absent: off the map).</summary>
        public readonly Dictionary<string, string> RivalModes = new();
        /// <summary>The rival car's state ("off" while it is not on the map), its heading as a world direction.</summary>
        public string RivalCarState { get; private set; } = "off";
        Vector3? rivalHeading;

        void RivalViewsInit()
        {
            foreach (var k in RivalKeys) Person(k, host.rivalBodyColour);
            Box("car/rivals", new Vector3(1.8f, 1.4f, 4.6f), host.carColour, true); // the click box (1.8 x 1.4 x 4.6 m) as the sedans'
            // brief 067 L (Sky on 0.2.122; Vela's briefs/067/vela.md): Vela's Sedan_Rival on the sedans' footprint, pivot and nose (carNose stays
            // identity), its own nine materials as built. The official sedan in a multiplied "stand-in trim" is gone: one property block per renderer
            // wrote slot 0's teal to all ten slots, so it read as Vale's car in flat navy (Vela's finding). No model: the box stays the car
            if (Visual("car/rivals", host.sedanRival) is Transform sr) carModels["car/rivals"] = sr;
        }

        /// <summary>For the proofs: the kit model drawn for a car's view ("" when it is the box).</summary>
        public string CarModelName(string key) => modelSource.TryGetValue(key, out var n) ? n : "";

        /// <summary>Per tick: the five and the car from RivalPoses(); a member with a body (taken down) is left to the body view.</summary>
        void RivalViews(Game g)
        {
            RivalModes.Clear();
            List<(string id, Pt pos, Pt facing, string mode)> members; (Pt pos, Pt heading, string state) car;
            try { (members, car) = g.RivalPoses(); } catch { members = new List<(string, Pt, Pt, string)>(); car = (default, new Pt(0, 100), "off"); }
            RivalCarState = car.state;
            bool carOn = car.state != "off";
            bool leaving = TakenLeaving("car/rivals"); // engine 0.53.0 (car.take): ours and gone, it drives off in the view (HostViews.LeavingLent)
            if (!leaving) { Set("car/rivals", carOn ? car.pos : (Pt?)null); rivalHeading = null; }
            if (!leaving && carOn && (car.heading.X != 0 || car.heading.Y != 0)) { var w = frame.World(new Pt(car.pos.X + car.heading.X, car.pos.Y + car.heading.Y)) - frame.World(car.pos); w.y = 0f; if (w.sqrMagnitude > 1e-8f) rivalHeading = w.normalized; }
            foreach (var k in RivalKeys) RivalModes.Remove(k);
            var seen = new HashSet<string>();
            foreach (var m in members)
            {
                if (!things.ContainsKey(m.id)) continue; seen.Add(m.id); RivalModes[m.id] = m.mode;
                if (m.mode == "aboard") { Set(m.id, null); continue; }
                if (m.mode == "down")
                {
                    if (g.BodyById(m.id) is Body) { Set(m.id, null); continue; } // taken down: the body view draws him (Bodies)
                    Set(m.id, m.pos, true, Radius); continue;                         // shot down: where he fell
                }
                Set(m.id, m.pos);
                if (m.mode != "walking" && (m.facing.X != 0 || m.facing.Y != 0)) faces[m.id] = frame.Dir(m.facing);
            }
            foreach (var k in RivalKeys) if (!seen.Contains(k)) Set(k, null);
        }

        /// <summary>The rival car's engine heading as a world direction, else null (it keeps its last).</summary>
        Vector3? RivalCarFacing() => rivalLeaveHeading ?? (TakenLeaving("car/rivals") ? lentHeading : rivalHeading); // brief 109 round 2: along its way out after the end (HostViews.LeavingRivals)

        /// <summary>The getaway car's engine heading as a world direction (along its Drive, else its stand's facing).</summary>
        Vector3? GetawayFacing()
        {
            if (leaveHeading is Vector3 lh) return lh; // brief 079: along the way out, once it has left
            var g = host.Game; if (g == null || g.Range) return null; Pt h, at;
            try { h = g.GetawayHeading(); at = g.GetawayCarPos(); } catch { return null; }
            if (h.X == 0 && h.Y == 0) return null;
            var w = frame.World(new Pt(at.X + h.X, at.Y + h.Y)) - frame.World(at); w.y = 0f;
            return w.sqrMagnitude > 1e-8f ? w.normalized : null;
        }

        /// <summary>Where the rival crew is best anchored for a chip, a speech bubble or a beat's pointer: the car when it is on the street, else
        /// the first member drawn, else the car's key (hidden: the anchor reads null).</summary>
        public string RivalAnchorKey()
        {
            if (things.TryGetValue("car/rivals", out var c) && c.shown) return "car/rivals";
            foreach (var k in RivalKeys) if (things.TryGetValue(k, out var m) && m.shown) return k;
            return "car/rivals";
        }

        /// <summary>The members drawn now, with their modes (the proof and the HUD's rival marks).</summary>
        public IEnumerable<(string id, Vector3 at, string mode)> RivalsDrawn()
        {
            foreach (var k in RivalKeys) if (things.TryGetValue(k, out var m) && m.shown && m.t && m.t.gameObject.activeInHierarchy) yield return (k, m.t.position, RivalModes.TryGetValue(k, out var md) ? md : "");
        }
    }
}
