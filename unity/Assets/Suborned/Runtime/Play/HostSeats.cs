using System.Collections.Generic;
using System.IO;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>The café chairs (Pulse, contracts/people/handover.seats.json, suborned.view.seats/1, copied beside the sim bundle by
    /// `bun tools/sim/unity.ts`): each chair's point and the way it faces, by the engine's place name `cafe-table-N-seat-K`. View only and
    /// unhashed. When someone sits is the engine's own: a walker's stop at that place, on the street (Walker.From/To, ticks), so the view
    /// never disagrees with where the engine walks them (the file's from/to are mission seconds, though its note says ticks). Absent or
    /// unreadable: nobody is drawn seated.</summary>
    public sealed class SeatData
    {
        public readonly Dictionary<string, (Pt at, Pt face)> Chairs = new();

        public static SeatData Load()
        {
            var path = System.IO.Path.Combine(SimBundle.Dir, "handover.seats.json"); if (!File.Exists(path)) return null;
            var j = JNode.Parse(File.ReadAllText(path), "handover.seats.json");
            if (j["schema"].Str() != "suborned.view.seats/1") throw new InvalidDataException("handover.seats.json is not suborned.view.seats/1");
            var d = new SeatData();
            foreach (var t in j["tables"].Entries)
                foreach (var c in t.Value["chairs"].Items) { var a = c["at"].Ints(); var f = c["face"].Ints(); d.Chairs[$"{t.Key}-seat-{c["chair"].Int()}"] = (new Pt(a[0], a[1]), new Pt(f[0], f[1])); }
            return d;
        }
    }

    public sealed partial class HandoverHost
    {
        /// <summary>The café seats (HostSeats), loaded with the bundle; null when there are none.</summary>
        public SeatData CafeSeats { get; private set; }
        bool seatsTried;
        void LoadSeats() { if (seatsTried) return; seatsTried = true; try { CafeSeats = SeatData.Load(); } catch (System.Exception e) { CafeSeats = null; Debug.LogWarning($"Seats: {e.Message}; nobody is drawn seated"); } }
    }

    public sealed partial class HostViews
    {
        Dictionary<string, int> walkerByKey; // crowd key → index into the game's walkers (fresh each game)

        /// <summary>A crowd view on its chair now: the chair's world point, the way it faces (to the table) and the stop's ticks, else null.
        /// Seated = within a stop at a café chair and drawn within 0.35 m of it (the engine walks them there).</summary>
        (Vector3 at, float yaw, int from, int to)? SeatNow(string key, Vector3 drawn, Game g)
        {
            if (host.CafeSeats == null || !key.StartsWith("crowd/")) return null;
            if (walkerByKey == null) { walkerByKey = new(); for (int i = 0; i < g.Walkers.Count && i < crowdKeys.Length; i++) walkerByKey[crowdKeys[i]] = i; }
            if (!walkerByKey.TryGetValue(key, out var wi) || wi >= g.Walkers.Count) return null; var w = g.Walkers[wi];
            if (w.St.Length > 0) return null; // startled, fleeing, sheltered: off the routine
            for (int i = 0; i < w.Place.Length; i++)
            {
                if (g.Tick < w.From[i] || g.Tick >= w.To[i]) continue;
                if (!w.Out[i] || !host.CafeSeats.Chairs.TryGetValue(w.Place[i], out var ch)) return null;
                var at = frame.World(ch.at); var d = at - drawn; d.y = 0f; if (d.magnitude > 0.35f) return null;
                var fw = frame.World(new Pt(ch.at.X + ch.face.X, ch.at.Y + ch.face.Y)) - at;
                return (new Vector3(at.x, drawn.y, at.z), Mathf.Atan2(fw.x, fw.z) * Mathf.Rad2Deg, w.From[i], w.To[i]);
            }
            return null;
        }

        /// <summary>Forma's seated clips (tools/figures/seat.json): stand_to_sit for its 2.3 s from the tick the figure is first drawn on the
        /// chair after walking to it, then one of three loops picked by the figure's phase (sit, sit_alt, sit_talk, so a table does not move in step), and
        /// sit_to_stand over the last 2.3 s of the span, ending standing over the seat as the engine walks them off. Ticks, not frames: pause
        /// and speed hold it, and the same run draws the same.</summary>
        string SeatState(Fig f, int from, int to, Game g)
        {
            if (f.sitTick < 0 || f.sitTick > g.Tick) f.sitTick = f.state is "walk" or "walk_f" or "walk_old" or "run" ? g.Tick : g.Tick - SitTicks; // sits down only when walked there; seated at the opening or after a skip: already down
            if (SeatTransitions && Has(f, "sit_to_stand") && to - g.Tick <= SitTicks) return "sit_to_stand";
            if (SeatTransitions && Has(f, "stand_to_sit") && g.Tick - f.sitTick < SitTicks) return "stand_to_sit";
            int i = (int)(f.phase * 3f) % 3; var loop = i == 1 ? "sit_alt" : i == 2 ? "sit_talk" : "sit";
            return Has(f, loop) ? loop : "sit";
        }
        const int SitTicks = 23; // the clips' 2.3 s, in the engine's tenths
        /// <summary>Play the sit-down and stand-up clips (true), or snap straight to seated and standing (false, the default). Off since brief 022
        /// (Latch, 2026-10-19): at play zoom 14/38 the figure stands inside the chair for about a second of each clip, and both clips' standing
        /// end is 0.11 to 0.15 m above the idle stance (hips 1.06 to 1.10 m against 0.95 m), a visible drop on the switch to walk. The 0.15 s
        /// cross-fade between walk and the seated loop does the sitting. Turn on again when Forma's clips clear the chair.</summary>
        public static bool SeatTransitions = false;
    }
}
