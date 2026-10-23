using System.Collections.Generic;
using UnityEngine;

namespace Suborned
{
    /// <summary>The cut the architecture fade makes this frame, as the shaders test it (DK_Cutaway.hlsl).</summary>
    public struct FadeCut
    {
        /// <summary>The focus (world): the disc lies round it, at its height.</summary>
        public Vector3 Focus;
        /// <summary>Height (world y) at or below which nothing is cut.</summary>
        public float FloorY;
        /// <summary>Ground run (x, z) of a view ray per metre it drops.</summary>
        public Vector2 Run;
        /// <summary>The view on the ground (x, z), unit.</summary>
        public Vector2 Ahead;
        /// <summary>How far beyond the focus, along Ahead, the cut reaches (m).</summary>
        public float Back;
        /// <summary>Disc radius (m).</summary>
        public float Radius;

        public bool Same(in FadeCut o) => Focus == o.Focus && FloorY == o.FloorY && Run == o.Run && Ahead == o.Ahead && Back == o.Back && Radius == o.Radius;
    }

    /// <summary>The tagged renderers' bounds on a 16 m grid in plan (x, z), the builder's fade cells (kit.json fadeCell), so a cut
    /// tests only what it could reach (Redline 2026-10-15 M5). Each renderer is listed in every cell its footprint overlaps; each
    /// cell keeps the union of its members' bounds. A query walks only the cells under the cut's slanted column (per axis, the disc
    /// swept back along the view from the floor to the tallest top: the region Reaches' own box test can accept), skips a cell
    /// whose union cannot be reached (Reaches is monotone: a box inside one the cut cannot reach is not reached either), and tests
    /// the members of the rest. Same answer as testing every renderer. Pure geometry:
    /// no Unity objects, no allocation per query.</summary>
    public sealed class FadeGrid
    {
        public const float Cell = 16f;
        readonly Bounds[] bounds;
        readonly Dictionary<long, int> cells = new();
        readonly int[] start, items;
        readonly Bounds[] cellBounds;
        readonly int[] seen;
        readonly float top;
        int stamp;

        /// <summary>Renderers indexed (0 = none).</summary>
        public int Count => bounds.Length;
        /// <summary>Cells the last query visited, and renderers it tested.</summary>
        public int CellsVisited { get; private set; }
        public int Tested { get; private set; }

        public FadeGrid(Bounds[] bounds)
        {
            this.bounds = bounds;
            seen = new int[bounds.Length];
            top = float.NegativeInfinity;
            var members = new List<List<int>>();
            for (int i = 0; i < bounds.Length; i++)
            {
                var b = bounds[i];
                top = Mathf.Max(top, b.max.y);
                int x0 = Index(b.min.x), x1 = Index(b.max.x), z0 = Index(b.min.z), z1 = Index(b.max.z);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        long key = Key(x, z);
                        if (!cells.TryGetValue(key, out int c)) { cells[key] = c = members.Count; members.Add(new List<int>()); }
                        members[c].Add(i);
                    }
            }
            start = new int[members.Count + 1];
            cellBounds = new Bounds[members.Count];
            int n = 0;
            for (int c = 0; c < members.Count; c++) n += members[c].Count;
            items = new int[n];
            n = 0;
            for (int c = 0; c < members.Count; c++)
            {
                start[c] = n;
                var u = bounds[members[c][0]];
                foreach (int i in members[c]) { items[n++] = i; u.Encapsulate(bounds[i]); }
                cellBounds[c] = u;
            }
            start[members.Count] = n;
        }

        static int Index(float v) => Mathf.FloorToInt(v / Cell);
        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        /// <summary>Appends to `into` every renderer the cut reaches within r (each once).</summary>
        public void Query(in FadeCut cut, float r, List<int> into)
        {
            CellsVisited = 0; Tested = 0;
            if (++stamp == int.MaxValue) { System.Array.Clear(seen, 0, seen.Length); stamp = 1; }
            float lo = cut.FloorY - cut.Focus.y, hi = top - cut.Focus.y;
            if (bounds.Length == 0 || hi <= lo) return;
            // Reaches tests the box of the footprint swept by run * t, t in [lo, its own top]: per axis, a footprint that can reach
            // overlaps [focus - max(run lo, run hi) - r, focus - min(run lo, run hi) + r] with the tallest top as hi
            int x0 = Index(cut.Focus.x - Mathf.Max(cut.Run.x * lo, cut.Run.x * hi) - r), x1 = Index(cut.Focus.x - Mathf.Min(cut.Run.x * lo, cut.Run.x * hi) + r);
            int z0 = Index(cut.Focus.z - Mathf.Max(cut.Run.y * lo, cut.Run.y * hi) - r), z1 = Index(cut.Focus.z - Mathf.Min(cut.Run.y * lo, cut.Run.y * hi) + r);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    if (!cells.TryGetValue(Key(x, z), out int c)) continue;
                    CellsVisited++;
                    if (!Reaches(cellBounds[c], cut, r)) continue;
                    for (int k = start[c]; k < start[c + 1]; k++)
                    {
                        int i = items[k];
                        if (seen[i] == stamp) continue;
                        seen[i] = stamp; Tested++;
                        if (Reaches(bounds[i], cut, r)) into.Add(i);
                    }
                }
        }

        /// <summary>Whether a fragment inside b can be cut: some part above the floor, on the camera's side of the back limit,
        /// whose view ray (or, in column mode, the run of rays below it) lands within r of the focus.</summary>
        public static bool Reaches(in Bounds b, in FadeCut c, float r)
        {
            Vector3 min = b.min, max = b.max;
            if (max.y <= c.FloorY) return false;
            // the least of dot(p - focus, ahead) over the box: at or past the back limit, all of it stands beyond
            float least = Mathf.Min(c.Ahead.x * (min.x - c.Focus.x), c.Ahead.x * (max.x - c.Focus.x)) + Mathf.Min(c.Ahead.y * (min.z - c.Focus.z), c.Ahead.y * (max.z - c.Focus.z));
            if (least >= c.Back) return false;
            // where the box's rays land on the focus level: its footprint swept along the view from the floor to its top
            float lo = c.FloorY - c.Focus.y, hi = max.y - c.Focus.y;
            float dx = Gap(min.x + Mathf.Min(c.Run.x * lo, c.Run.x * hi), max.x + Mathf.Max(c.Run.x * lo, c.Run.x * hi), c.Focus.x);
            float dz = Gap(min.z + Mathf.Min(c.Run.y * lo, c.Run.y * hi), max.z + Mathf.Max(c.Run.y * lo, c.Run.y * hi), c.Focus.z);
            return dx * dx + dz * dz < r * r;
        }

        static float Gap(float lo, float hi, float v) => v < lo ? lo - v : v > hi ? v - hi : 0f;
    }

    /// <summary>Keeps the cost of the architecture fade to what it can reach. A shader that may discard costs the GPU its
    /// hidden-surface removal on every pixel it covers, so the tagged architecture draws with its plain variant, and only the
    /// renderers the cut can reach (their bounds against the cut's slanted column, with a margin) take a twin of their
    /// material with the cutting variant on (keyword _DK_CUT). The shaders still decide per pixel what thins: a renderer
    /// changing variant is not seen. Renderer count and material count per renderer never change, so the GPU Resident Drawer
    /// keeps every instance; a pan moves a handful of renderers from one material to its twin. Per frame the focus moves, only
    /// the grid cells under the cut and the renderers reached last frame are touched (FadeGrid), and nothing is allocated: the
    /// twins and their arrays are made at construction. Play Mode only: the twins are runtime objects and must never be saved
    /// into the generated scene.</summary>
    public sealed class FadeReach
    {
        public const string Keyword = "_DK_CUT";
        /// <summary>Reach beyond the disc (m): a renderer takes its cutting variant before the cut gets to it, so a focus that
        /// moves after this frame's reach was set (or a lag of a frame) shows no uncut edge.</summary>
        const float Margin = 4f;

        readonly MeshRenderer[] renderers;
        readonly Bounds[] bounds;
        readonly Material[][] plain, cutting;
        readonly bool[] reached;
        readonly FadeGrid grid;
        /// <summary>Per renderer, its batch tile's group (fade cells `mat@tx,tz#cx,cz` of one tile share it; others stand alone).</summary>
        readonly int[] group;
        readonly bool[] groupHit;
        readonly Bounds[] groupBounds;
        bool lastWhole;
        readonly Dictionary<Material, Material> twins = new();
        /// <summary>The renderers drawing with the cutting variant (this frame's and last frame's, swapped).</summary>
        List<int> on = new(), next = new();
        /// <summary>Per renderer, the last Set that reached it (`frame`).</summary>
        readonly int[] now;
        /// <summary>Per renderer, when a cut last reached it (unscaled s); the linger (F6).</summary>
        readonly float[] lastIn;
        const float Linger = 1.5f;
        int frame;
        bool any;

        /// <summary>Tagged props drawn whole instead (wholeProps).</summary>
        public int WholeProps { get; private set; }

        static bool IsProp(MeshRenderer r, string[] prefixes)
        {
            if (prefixes == null || prefixes.Length == 0) return false;
            var n = r.name.StartsWith("__LOD", System.StringComparison.Ordinal) && r.transform.parent ? r.transform.parent.name : r.name;
            foreach (var p in prefixes) if (!string.IsNullOrEmpty(p) && n.StartsWith(p, System.StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Tagged renderers in the loaded scenes.</summary>
        public int Count => renderers.Length;
        /// <summary>How many of them draw with the cutting variant now.</summary>
        public int Reached { get; private set; }

        /// <summary>wholeProps: kit pieces (by name prefix, a LOD child by its piece's name) drawn whole inside the discs although the builder
        /// tagged them (Glyph's 016 review §2: cypresses and parasols in a disc dithered into dotted columns that no longer read as trees; only
        /// building meshes fade). Their tag is taken off for the session (Play only: never saved).</summary>
        public FadeReach(string[] wholeProps = null)
        {
            var tagged = new List<MeshRenderer>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
                if ((r.renderingLayerMask & ArchitectureFade.Layer) != 0)
                {
                    if (IsProp(r, wholeProps)) { r.renderingLayerMask &= ~ArchitectureFade.Layer; WholeProps++; continue; }
                    tagged.Add(r);
                }
            renderers = tagged.ToArray();
            bounds = new Bounds[renderers.Length]; plain = new Material[renderers.Length][]; cutting = new Material[renderers.Length][]; reached = new bool[renderers.Length]; now = new int[renderers.Length]; lastIn = new float[renderers.Length];
            var shared = new Dictionary<Material, Material[]>(); // one twin array per single-material renderer kind: few
            for (int i = 0; i < renderers.Length; i++)
            {
                bounds[i] = renderers[i].bounds; plain[i] = renderers[i].sharedMaterials;
                if (plain[i].Length == 1 && plain[i][0])
                {
                    if (!shared.TryGetValue(plain[i][0], out cutting[i])) shared[plain[i][0]] = cutting[i] = Twins(plain[i]);
                }
                else cutting[i] = Twins(plain[i]);
            }
            grid = new FadeGrid(bounds);
            group = new int[renderers.Length];
            var groups = new Dictionary<string, int>(System.StringComparer.Ordinal);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i]; int hash = r.name.IndexOf('#');
                if (hash < 0) { group[i] = groups.Count; groups[$"\u0001{i}"] = group[i]; continue; }
                var key = (r.transform.parent ? r.transform.parent.name : "") + "/" + r.name[..hash];
                if (!groups.TryGetValue(key, out group[i])) groups[key] = group[i] = groups.Count;
            }
            groupHit = new bool[groups.Count]; groupBounds = new Bounds[groups.Count];
            var seen = new bool[groups.Count];
            for (int i = 0; i < renderers.Length; i++)
            {
                int g = group[i];
                if (!seen[g]) { groupBounds[g] = bounds[i]; seen[g] = true; } else groupBounds[g].Encapsulate(bounds[i]);
            }
        }

        /// <summary>Gives the cutting variant to what the cut can reach and takes it back from what it no longer can. wholeTiles:
        /// a reached fade cell takes every cell of its batch tile with it (the reach before 008 B6, for the perf harness; it walks
        /// every renderer, as the reach before B6 did).</summary>
        public void Set(in FadeCut cut, bool wholeTiles = false) { one[0] = cut; Set(one, 1, wholeTiles); }

        readonly FadeCut[] one = new FadeCut[1];
        FadeCut[] lastCuts = new FadeCut[1];
        int lastCount;

        /// <summary>As Set, for several discs at once (the focus and the points ArchitectureFade keeps clear, brief 016): what any of the
        /// first `count` cuts can reach takes the cutting variant.</summary>
        public void Set(FadeCut[] cuts, int count, bool wholeTiles = false)
        {
            if (any && wholeTiles == lastWhole && count == lastCount)
            {
                bool same = true; for (int c = 0; c < count && same; c++) same = cuts[c].Same(lastCuts[c]);
                if (same) return;
            }
            if (lastCuts.Length < count) lastCuts = new FadeCut[cuts.Length];
            System.Array.Copy(cuts, lastCuts, count); lastCount = count; any = true; lastWhole = wholeTiles;
            next.Clear();
            if (++frame == int.MaxValue) { System.Array.Clear(now, 0, now.Length); frame = 1; }
            for (int c = 0; c < count; c++)
            {
                var cut = cuts[c]; float r = cut.Radius + Margin; int from = next.Count;
                if (!wholeTiles) grid.Query(cut, r, next);
                else
                {
                    for (int g = 0; g < groupHit.Length; g++) groupHit[g] = FadeGrid.Reaches(groupBounds[g], cut, r); // the tile's bounds, as its one renderer had
                    for (int i = 0; i < renderers.Length; i++) if (groupHit[group[i]]) next.Add(i);
                }
                // keep each renderer once across the discs
                int w = from; for (int k = from; k < next.Count; k++) { int i = next[k]; if (now[i] == frame) continue; now[i] = frame; next[w++] = i; }
                next.RemoveRange(w, next.Count - w);
            }
            // brief 045 F6 (Redline: the GPU Resident Drawer rebuilt its draw lists every frame late; a material swap is an instance destroyed
            // and made again): a renderer leaving every reach keeps its cutting twin for Linger seconds (it draws the same with no cut near), so
            // the discs of people walking past a tile's edge do not swap its materials back and forth every frame
            float t = Time.unscaledTime;
            foreach (int i in next) { Mark(i, true); lastIn[i] = t; }
            foreach (int i in on) if (now[i] != frame) { if (t - lastIn[i] < Linger) { now[i] = frame; next.Add(i); } else Mark(i, false); }
            (on, next) = (next, on);
        }

        /// <summary>Nothing fades: every renderer back to its plain material.</summary>
        public void Clear()
        {
            if (!any) return;
            any = false;
            foreach (int i in on) Mark(i, false);
            on.Clear();
        }

        /// <summary>Puts the plain materials back and destroys the twins.</summary>
        public void Dispose()
        {
            Clear();
            foreach (var twin in twins.Values) if (twin) Object.Destroy(twin);
            twins.Clear();
        }

        void Mark(int i, bool to)
        {
            if (reached[i] == to) return;
            reached[i] = to; Reached += to ? 1 : -1;
            if (!renderers[i]) return;
            renderers[i].sharedMaterials = to ? cutting[i] : plain[i];
        }

        /// <summary>The cutting twins of these materials (shared with the reach's own, destroyed with it).</summary>
        public Material[] CuttingOf(Material[] of) => Twins(of);

        Material[] Twins(Material[] of)
        {
            var to = new Material[of.Length];
            for (int i = 0; i < of.Length; i++) to[i] = Twin(of[i]);
            return to;
        }

        Material Twin(Material m)
        {
            if (!m || !m.shader.keywordSpace.FindKeyword(Keyword).isValid) return m; // glass and the like: no cutting variant
            if (!twins.TryGetValue(m, out var twin))
            {
                twins[m] = twin = new Material(m) { name = m.name, hideFlags = HideFlags.HideAndDontSave };
                twin.EnableKeyword(Keyword);
            }
            return twin;
        }
    }
}
