using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 023 (Pulse's trace: 31 clerks on one engine point drawn as a sunflower lattice, all facing one way): where the crowd is
    /// drawn when the engine puts several people on one point. View only; the engine is not told. Everything is per engine tick and seeded
    /// by the walker's id, never by frame time, so the same run draws the same.
    ///  · standing together: knots of one to four, each person on a spot from their own hash-seeded sequence, at least SpreadCm from everyone
    ///    already placed (KnotGapCm between knots) and on free ground; earlier arrivals keep their spots (order: tick of arrival, then the
    ///    hash). A group with a wall or doorway within WallReachCm draws up to it (WallStandCm off) and spreads along it. A knot faces its
    ///    middle; one alone faces out from the wall.
    ///  · walking together: a loose column along the direction of travel (rows RowCm apart, up to three abreast LaneCm apart, staggered,
    ///    jittered and drifting by tick and hash).
    ///  · startled, or fleeing with nowhere to run: facing away from the last shot.
    /// Offsets move at most StepCm a tick (a walk), so nobody jumps when a group changes.</summary>
    public sealed partial class HostViews
    {
        const int SpreadCm = 72, KnotGapCm = 150, RowCm = 140, LaneCm = 90, WallReachCm = 400, WallStandCm = 90, StepCm = 14;
        Game spreadGame;
        Pt?[] prevAt = System.Array.Empty<Pt?>(); Pt?[] atNow = System.Array.Empty<Pt?>(); Vector2[] offCur = System.Array.Empty<Vector2>(), offWant = System.Array.Empty<Vector2>();
        Vector2?[] spot = System.Array.Empty<Vector2?>(); int[] spotSince = System.Array.Empty<int>(); Pt?[] spotPt = System.Array.Empty<Pt?>();
        int[] since = System.Array.Empty<int>(); Pt?[] sincePt = System.Array.Empty<Pt?>(); uint[] idHash = System.Array.Empty<uint>(); Vector2?[] faceWant = System.Array.Empty<Vector2?>();
        readonly Dictionary<Pt, List<int>> groups = new(); readonly List<List<int>> listPool = new(); readonly List<Vector2> placed = new();
        readonly Dictionary<Pt, (Vector2 anchor, Vector2 u, bool wall)> wallAt = new();
        /// <summary>Where the last shot came from (engine cm, quantised to 1 m), set from the engine's log as it is read.</summary>
        public Pt? LastShotFrom { get; set; }

        void SpreadInit(Game g)
        {
            int n = g.Walkers.Count; spreadGame = g;
            prevAt = new Pt?[n]; atNow = new Pt?[n]; offCur = new Vector2[n]; offWant = new Vector2[n]; since = new int[n]; sincePt = new Pt?[n]; faceWant = new Vector2?[n]; spot = new Vector2?[n]; spotSince = new int[n]; spotPt = new Pt?[n];
            idHash = new uint[n]; for (int i = 0; i < n; i++) { uint h = 2166136261; foreach (var c in g.Walkers[i].P.Id) { h ^= c; h *= 16777619; } idHash[i] = h; }
            LastShotFrom = null;
        }

        static uint Mix(uint h, uint a) { h ^= a * 0x9E3779B9u; h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16; return h; }
        static float U01(uint h) => (h & 0xFFFFFF) / 16777216f;
        static Vector2 V(Pt p) => new(p.X, p.Y);
        static Pt P(Vector2 v) => new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        bool FreeAt(Game g, Vector2 v) => g.S.Grid.Free(P(v));

        /// <summary>The crowd's drawn points this tick (engine cm), and each person's wanted facing (engine axes) where the spread sets one.</summary>
        void Spread(Game g)
        {
            if (spreadGame != g || prevAt.Length != g.Walkers.Count) SpreadInit(g);
            int n = g.Walkers.Count;
            foreach (var l in groups.Values) { l.Clear(); listPool.Add(l); }
            groups.Clear();
            for (int i = 0; i < n; i++)
            {
                var at = g.CrowdPos(g.Walkers[i]); atNow[i] = at; offWant[i] = Vector2.zero; faceWant[i] = null;
                if (!at.HasValue) { prevAt[i] = null; offCur[i] = Vector2.zero; sincePt[i] = null; continue; }
                if (!sincePt[i].HasValue || !sincePt[i].Value.Equals(at.Value)) { sincePt[i] = at; since[i] = g.Tick; }
                if (!groups.TryGetValue(at.Value, out var list)) { if (listPool.Count > 0) { list = listPool[^1]; listPool.RemoveAt(listPool.Count - 1); } else list = new List<int>(); groups[at.Value] = list; }
                list.Add(i);
            }
            placed.Clear();
            foreach (var kv in groups) if (kv.Value.Count == 1 && prevAt[kv.Value[0]] is { } pv && pv.Equals(kv.Key)) placed.Add(V(kv.Key)); // standing singles keep their own point (a walker passing by moves nobody)
            foreach (var kv in groups)
            {
                var m = kv.Value; if (m.Count < 2) continue;
                m.Sort((a, b) => since[a] != since[b] ? since[a].CompareTo(since[b]) : idHash[a] != idHash[b] ? idHash[a].CompareTo(idHash[b]) : a.CompareTo(b));
                var at = V(kv.Key);
                // walking together: the direction of travel, from the first member's last point (they share it)
                Vector2 dir = Vector2.zero;
                foreach (var i in m) if (prevAt[i].HasValue && !prevAt[i].Value.Equals(kv.Key)) { dir = (at - V(prevAt[i].Value)).normalized; break; }
                if (dir != Vector2.zero) Column(g, m, at, dir, g.Tick); else Stand(g, kv.Key, m, at);
            }
            // startled (or fleeing with nowhere to run): away from the last shot
            if (LastShotFrom is { } shot)
                for (int i = 0; i < n; i++)
                {
                    var w = g.Walkers[i]; if (!atNow[i].HasValue) continue;
                    bool still = !prevAt[i].HasValue || prevAt[i].Value.Equals(atNow[i].Value);
                    if (w.St == "startled" || (w.St == "fleeing" && still)) { var d = V(atNow[i].Value) - V(shot); if (d.sqrMagnitude > 1f) faceWant[i] = d.normalized; }
                }
            for (int i = 0; i < n; i++)
            {
                var d = offWant[i] - offCur[i]; offCur[i] = d.magnitude <= StepCm ? offWant[i] : offCur[i] + d.normalized * StepCm;
                prevAt[i] = atNow[i];
            }
        }

        /// <summary>A loose column along the direction of travel: rows RowCm apart, up to three abreast LaneCm apart, each lane staggered and
        /// each person a little off their slot and drifting slowly back and forth along it (by tick and hash), so a party walks, not marches.
        /// Worst case within a lane RowCm − 2·(15 + 12) = 86 cm; across lanes LaneCm − 2·8 = 74 cm.</summary>
        void Column(Game g, List<int> m, Vector2 at, Vector2 dir, int tick)
        {
            int count = m.Count, lanes = count <= 2 ? 1 : count <= 8 ? 2 : 3, rows = (count + lanes - 1) / lanes; var side = new Vector2(-dir.y, dir.x);
            for (int j = 0; j < count; j++)
            {
                int i = m[j], r = j / lanes, l = j % lanes; uint h = idHash[i];
                float period = 40f + 30f * U01(Mix(h, 6)), drift = 12f * Mathf.Sin((tick / period + U01(Mix(h, 8))) * 6.2831853f);
                float along = ((rows - 1) * 0.5f - r) * RowCm - l * 40f + (U01(Mix(h, 1)) - 0.5f) * 30f + drift;
                float lat = (l - (lanes - 1) * 0.5f) * LaneCm + (U01(Mix(h, 2)) - 0.5f) * 16f;
                var o = dir * along + side * lat;
                if (!FreeAt(g, at + o)) o = dir * along;           // no room abreast here: single file
                if (!FreeAt(g, at + o)) o = Vector2.zero;          // nor behind: on the point (the engine's own)
                offWant[i] = o;
            }
        }

        /// <summary>A standing group: hash-seeded spots at least SpreadCm apart on free ground, drawn up to a wall or doorway when there is one.</summary>
        void Stand(Game g, Pt key, List<int> m, Vector2 at)
        {
            if (!wallAt.TryGetValue(key, out var wa)) wallAt[key] = wa = Wall(g, at);
            var t = new Vector2(-wa.u.y, wa.u.x);
            // knots of one to four (sizes by the first member's hash: 15 / 35 / 30 / 20 %), so a crowd reads as people talking, not a block;
            // a knot's first member keeps KnotGapCm from everyone placed, the rest join within a step of them
            // a spot, once taken, is kept while its owner stays on this point (a newcomer never shuffles the others)
            foreach (var i in m) if (spot[i].HasValue && spotPt[i] is { } sp && sp.Equals(key) && spotSince[i] == since[i]) placed.Add(spot[i].Value); else spot[i] = null;
            for (int j = 0; j < m.Count;)
            {
                float r = U01(Mix(idHash[m[j]], 7)); int size = r < 0.15f ? 1 : r < 0.5f ? 2 : r < 0.8f ? 3 : 4; size = Mathf.Min(size, m.Count - j);
                for (int q = 0; q < size; q++)
                {
                    int i = m[j + q]; uint h = idHash[i]; Vector2? got = null;
                    if (spot[i] is { } kept) { offWant[i] = kept - at; continue; }
                    var from = q == 0 ? wa.anchor : spot[m[j]] ?? wa.anchor;
                    for (int a = 0; a < 80 && got == null; a++)
                    {
                        uint ha = Mix(h, (uint)(a + 11));
                        float th = U01(ha) * Mathf.PI * 2f;
                        float rho = q == 0 ? (j == 0 && a == 0 ? 0f : 60f * Mathf.Sqrt(a + 1 + U01(Mix(ha, 3)) * 2f)) : 78f + 14f * U01(Mix(ha, 3)) + 6f * a;
                        Vector2 o;
                        if (q == 0 && wa.wall) { float x = Mathf.Cos(th) * rho * 1.5f, y = -Mathf.Abs(Mathf.Sin(th)) * rho * 0.7f; o = t * x + wa.u * y; } // knots along the wall, never into it
                        else o = new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * rho;
                        var c = from + o;
                        if (!FreeAt(g, c)) continue;
                        float gap = q == 0 && a < 60 ? KnotGapCm : SpreadCm; bool clear = true;
                        foreach (var p in placed) if ((p - c).sqrMagnitude < gap * gap) { clear = false; break; }
                        if (clear) got = c;
                    }
                    var pos = got ?? at + new Vector2(Mathf.Cos((j + q) * 2.4f), Mathf.Sin((j + q) * 2.4f)) * 50f * Mathf.Sqrt(j + q); // nowhere clear: the old fan
                    placed.Add(pos); offWant[i] = pos - at; spot[i] = pos; spotPt[i] = key; spotSince[i] = since[i];
                }
                // facing: a knot faces its own middle; one alone faces out from the wall (or anywhere, by hash); each turned a little by their hash
                var mid = Vector2.zero; for (int q = 0; q < size; q++) mid += spot[m[j + q]].Value; mid /= size;
                for (int q = 0; q < size; q++)
                {
                    int i = m[j + q]; uint h = idHash[i]; var pos = spot[i].Value;
                    Vector2 f = size > 1 && (mid - pos).sqrMagnitude > 100f ? (mid - pos).normalized
                        : wa.wall ? -wa.u : new Vector2(Mathf.Cos(U01(Mix(h, 5)) * 6.283f), Mathf.Sin(U01(Mix(h, 5)) * 6.283f));
                    float jit = (U01(Mix(h, 4)) - 0.5f) * (size > 1 ? 50f : 80f) * Mathf.Deg2Rad;
                    faceWant[i] = new Vector2(f.x * Mathf.Cos(jit) - f.y * Mathf.Sin(jit), f.x * Mathf.Sin(jit) + f.y * Mathf.Cos(jit));
                }
                j += size;
            }
        }

        /// <summary>The nearest wall (or doorway: a building's edge on the grid) within WallReachCm of a point, by 16 rays over a clear line:
        /// the group's anchor WallStandCm off it, and the unit direction to it.</summary>
        static (Vector2 anchor, Vector2 u, bool wall) Wall(Game g, Vector2 at)
        {
            float best = float.MaxValue; Vector2 bu = Vector2.zero; int step = Mathf.Max(10, g.S.Grid.Cell / 2);
            for (int k = 0; k < 16; k++)
            {
                var u = new Vector2(Mathf.Cos(k * Mathf.PI / 8f), Mathf.Sin(k * Mathf.PI / 8f));
                for (int d = step; d <= WallReachCm; d += step)
                    if (!g.S.Grid.Free(P(at + u * d))) { if (d < best) { best = d; bu = u; } break; }
            }
            if (bu == Vector2.zero) return (at, Vector2.zero, false);
            float pull = Mathf.Max(0f, best - WallStandCm);
            return (at + bu * pull, bu, true);
        }

        /// <summary>The drawn point of crowd person i this tick (the engine's plus the eased spread offset).</summary>
        Pt SpreadAt(int i) => OldFan ? atNow[i].Value + OldFanAt(i) : atNow[i].Value + P(offCur[i]);
        /// <summary>For before/after shots only: the pre-023 fan (a 0.5 m sunflower by walker order, no facing).</summary>
        public static bool OldFan;
        Pt OldFanAt(int i)
        {
            int k = 0; for (int j = 0; j < i; j++) if (atNow[j].HasValue && atNow[j].Value.Equals(atNow[i].Value)) k++;
            float r = 50f * Mathf.Sqrt(k), th = k * 2.399963f; return new Pt(Mathf.RoundToInt(r * Mathf.Cos(th)), Mathf.RoundToInt(r * Mathf.Sin(th)));
        }
        /// <summary>The facing the spread wants for crowd person i (world axes), else null.</summary>
        Vector3? SpreadFace(int i) => !OldFan && faceWant[i] is { } f ? frame.Dir(P(f * 100f)) : (Vector3?)null;
    }
}
