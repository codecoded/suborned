using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Suborned.People;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.People
{
    /// <summary>The people layer (brief 010 E): a snapshot of out/people/&lt;plan&gt;.people.json (suborned.people/1, `bun run people`)
    /// at one clock time, built into the generated scene as the root `people`, one child per simulated person named by id. No
    /// simulation (Keel's C# sim steps routines later): each person stands at the routine stop whose [from, to) holds the clock, or,
    /// in the gap between two stops, on the straight line between them at the fraction of the gap (the walk graph the checker uses,
    /// tools/people/walk.ts, is not exported; a straight line is the snapshot's stand-in). A stop off the sheet ("beyond") is not drawn.
    /// Exteriors only (Sky, 2026-10-15): people inside (behind the door at work, or at home) are not drawn; each place with anyone
    /// inside gets an `inside: N` counter (InsideCount) the record panel tags at its door. People outdoors at one place stand on a
    /// sunflower spiral round it, every spot clear of the export's building, civic and landmark footprints. Background
    /// people are counts per plot in the file, not persons: none are drawn. Colours: people.json (civic look palette materials).
    /// Menu: Suborned/World/People/Snapshot 08:00 | 09:00 | 13:00 | 17:00. Batchmode: -executeMethod Suborned.World.People.PeopleLayer.Build
    /// [-people &lt;path&gt;] [-clock HH:MM] (saves the scene).</summary>
    public static class PeopleLayer
    {
        public const string RootName = "people";
        const string Here = "Assets/Suborned/Editor/World/People";
        static string SpecPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", Here, "people.json"));
        static string CivicLookPath => Path.Combine(HarbourWorldBuilder.RepoRoot, "contracts", "world", "civic-look.json");
        public static string PeoplePath => HarbourWorldBuilder.Arg("-people", Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "people", "harbour-district.people.json"));

        [MenuItem("Suborned/World/People/Snapshot 08:00")] static void At08() => Menu(8 * 60);
        [MenuItem("Suborned/World/People/Snapshot 09:00")] static void At09() => Menu(9 * 60);
        [MenuItem("Suborned/World/People/Snapshot 13:00")] static void At13() => Menu(13 * 60);
        [MenuItem("Suborned/World/People/Snapshot 17:00")] static void At17() => Menu(17 * 60);
        [MenuItem("Suborned/World/People/Remove")] static void RemoveMenu() { if (GameObject.Find(RootName) is { } g) Undo.DestroyObjectImmediate(g); }

        static void Menu(int clock) => HarbourWorldBuilder.Batch(() => { if (Review.RenderSession.EnsureScene()) Snapshot(clock); });

        /// <summary>Batchmode: opens the generated scene, builds the layer at -clock (default 17:00) and saves.</summary>
        public static void Build() => HarbourWorldBuilder.Batch(() =>
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-clock");
            int clock = i >= 0 && i + 1 < args.Length ? Minutes(args[i + 1]) : 17 * 60;
            EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            Snapshot(clock);
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new IOException("People: could not save the scene");
        });

        public static int Minutes(string hhmm)
        {
            var p = hhmm.Split(':');
            if (p.Length != 2 || !int.TryParse(p[0], out var h) || !int.TryParse(p[1], out var m) || h < 0 || h > 24 || m < 0 || m > 59) throw new ArgumentException($"People: clock `{hhmm}` is not HH:MM");
            return h * 60 + m;
        }

        public static string Clock(int min) => $"{min / 60 % 24:00}:{min % 60:00}";

        /// <summary>What the layer was built from and what it drew.</summary>
        public sealed class Result { public int Drawn, Walking, Inside, Beyond, Background; public string Sha; }

        /// <summary>Replaces the `people` root of the open scene with a snapshot at `clock` minutes. Throws on anything the spec does not know.</summary>
        public static Result Snapshot(int clock)
        {
            var path = PeoplePath;
            if (!File.Exists(path)) throw new FileNotFoundException($"People: {path} missing; run `bun run people`");
            var bytes = File.ReadAllBytes(path);
            var o = Json.Parse(new UTF8Encoding(false).GetString(bytes).TrimStart('﻿'));
            if (J.S(o, "schema") != "suborned.people/1") throw new InvalidDataException($"People: {path} is `{J.S(o, "schema")}`, not suborned.people/1");
            var spec = Json.Parse(File.ReadAllText(SpecPath));
            var palette = J.A(J.Get(Json.Parse(File.ReadAllText(CivicLookPath)), "palette"), "materials").ToDictionary(m => J.S(m, "material"), m => J.S(m, "unlit"));
            var civicLook = Json.Parse(File.ReadAllText(CivicLookPath));
            var passable = new HashSet<string>(J.A(civicLook, "overhead").Concat(J.A(civicLook, "frame")).Cast<string>());
            var world = WorldReader.Load(Review.RenderSession.ExportPath);
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            if (stamp && stamp.exportSha256 != world.Sha256) throw new InvalidDataException($"People: the scene was built from export {stamp.exportSha256[..12]}, the export on disk is {world.Sha256[..12]}; rebuild the world first");

            Color Hex(string material)
            {
                if (!palette.TryGetValue(material, out var hex)) throw new InvalidDataException($"People: material {material} is not in the civic look palette (CD-4 gates only that palette)");
                return ColorUtility.TryParseHtmlString(hex, out var c) ? c : throw new InvalidDataException($"People: bad hex {hex}");
            }
            float height = J.F(spec, "height"), radius = J.F(spec, "radius"), headD = J.F(spec, "head"), spacing = J.F(spec, "spacing"), clear = J.F(spec, "clearance");
            var headColour = Hex(J.S(spec, "headMaterial"));
            var blocks = J.O(J.Get(spec, "blocks")).ToDictionary(kv => kv.Key, kv => Hex((string)kv.Value));

            var places = J.A(o, "places").ToDictionary(p => J.S(p, "id"), p => (at: J.P(p, "at"), name: J.S(p, "name"), kind: J.S(p, "kind")));
            var units = J.A(o, "units").ToDictionary(u => J.S(u, "id"), u => J.S(u, "name"));
            var persons = J.A(o, "persons");
            var names = persons.ToDictionary(p => J.S(p, "id"), p => J.S(p, "name"));
            string WorkOf(object p) => J.Get(p, "work") is { } w ? $"{J.S(w, "position") ?? J.S(w, "role")}, {(units.TryGetValue(J.S(w, "employer"), out var en) ? en : J.S(w, "employer"))}" : "lives here";
            var jobs = persons.ToDictionary(p => J.S(p, "id"), WorkOf);
            var ground = new Ground(world, passable, clear);

            if (GameObject.Find(RootName) is { } old) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject(RootName);
            var picker = root.AddComponent<PeoplePicker>();
            picker.clock = Clock(clock);
            var res = new Result { Sha = Sha(bytes), Background = J.A(o, "background").Sum(b => (int)J.F(b, "residents") - (int)J.F(b, "simulated")) };
            picker.source = $"{Path.GetFileName(path)} {res.Sha[..12]}, {persons.Count} simulated, {res.Background} background (counts only, not drawn)";

            var mats = new Dictionary<string, Material>();
            var lit = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidDataException("People: URP Lit shader not found");
            Material Mat(string block)
            {
                if (mats.TryGetValue(block, out var m)) return m;
                if (!blocks.TryGetValue(block, out var c)) throw new InvalidDataException($"People: look block `{block}` is not in people.json (unknown = reject)");
                m = new Material(lit) { name = $"people/{block}", enableInstancing = true };
                m.SetColor("_BaseColor", c);
                m.SetFloat("_Smoothness", 0.35f);
                return mats[block] = m;
            }
            var headMat = new Material(lit) { name = "people/head", enableInstancing = true };
            headMat.SetColor("_BaseColor", headColour); headMat.SetFloat("_Smoothness", 0.35f);
            var capsuleMesh = Prim(PrimitiveType.Capsule);
            var sphereMesh = Prim(PrimitiveType.Sphere);

            // where each person is at `clock`
            var standing = new List<(object p, string place, bool indoors, int stop)>();
            var walking = new List<(object p, Vector2 at, string text)>();
            foreach (var p in persons)
            {
                var stops = J.A(J.Get(p, "routine"), "stops");
                var post = J.Get(p, "work") is { } w ? J.S(w, "post") : null;
                var workPlace = J.Get(p, "work") is { } w2 ? J.S(w2, "place") : null;
                int at = stops.FindIndex(s => In(clock, (int)J.F(s, "from"), (int)J.F(s, "to")));
                if (at >= 0)
                {
                    var s = stops[at]; var place = J.S(s, "place"); var doing = J.S(s, "doing");
                    if (place == "beyond") { res.Beyond++; continue; }
                    if (!places.ContainsKey(place)) throw new InvalidDataException($"People: {J.S(p, "id")} stops at unknown place {place}");
                    bool indoors = doing == "home" || (place == workPlace && post == "inside" && doing is "work" or "meal" or "pre-shift");
                    standing.Add((p, place, indoors, at));
                    continue;
                }
                int gap = -1;
                for (int k = 0; k < stops.Count; k++)
                {
                    int a = (int)J.F(stops[k], "to"), b = (int)J.F(stops[(k + 1) % stops.Count], "from");
                    if (In(clock, a, b)) { gap = k; break; }
                }
                if (gap < 0) throw new InvalidDataException($"People: {J.S(p, "id")} is neither at a stop nor between two at {Clock(clock)}");
                var s0 = stops[gap]; var s1 = stops[(gap + 1) % stops.Count];
                string p0 = J.S(s0, "place"), p1 = J.S(s1, "place");
                if (p0 == "beyond" || p1 == "beyond") { res.Beyond++; continue; } // off the sheet for part of the walk: no straight line on it
                int t0 = (int)J.F(s0, "to"), t1 = (int)J.F(s1, "from");
                float f = Mathf.Clamp01(Span(t0, clock) / Mathf.Max(1f, Span(t0, t1)));
                walking.Add((p, Vector2.Lerp(places[p0].at, places[p1].at, f), $"walking {places[p0].name} → {places[p1].name} ({f:P0}; straight line, the walk graph is not exported)"));
            }

            var counters = new GameObject("inside"); counters.transform.SetParent(root.transform, false);
            foreach (var g in standing.Where(x => x.indoors).GroupBy(x => x.place).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                var at = places[g.Key].at; var c = new GameObject(g.Key);
                c.transform.SetParent(counters.transform, false);
                c.transform.localPosition = new Vector3(at.x, ground.Height(at), at.y);
                var ic = c.AddComponent<InsideCount>(); ic.place = places[g.Key].name; ic.count = g.Count();
                res.Inside += ic.count;
            }
            foreach (var g in standing.Where(x => !x.indoors).GroupBy(x => x.place).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                var centre = places[g.Key].at; int k = 0;
                foreach (var (p, place, _, stop) in g.OrderBy(x => J.S(x.p, "id"), StringComparer.Ordinal))
                {
                    Vector2 spot;
                    for (; ; k++)
                    {
                        if (k > 4000) throw new InvalidDataException($"People: no free ground round {g.Key}");
                        float r = spacing * Mathf.Sqrt(k), th = k * 2.399963f;
                        spot = centre + new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * r;
                        if (!ground.Blocked(spot)) { k++; break; }
                    }
                    var s = J.A(J.Get(p, "routine"), "stops")[stop];
                    Person(p, spot, stop, $"{J.S(s, "doing")} at {places[place].name}");
                }
            }
            foreach (var (p, at, text) in walking) { Person(p, at, -1, text); res.Walking++; }
            picker.source += $", {res.Inside} inside (not drawn)";
            Debug.Log($"People: snapshot {Clock(clock)} from {path} (sha256 {res.Sha[..12]}): {res.Drawn} drawn ({res.Walking} walking), {res.Inside} inside at {counters.transform.childCount} places (not drawn), {res.Beyond} off the sheet, {res.Background} background people (counts only)");
            EditorSceneManager.MarkSceneDirty(root.scene);
            return res;

            void Person(object p, Vector2 at, int stop, string where)
            {
                var id = J.S(p, "id");
                var go = new GameObject(id);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(at.x, ground.Height(at), at.y);
                var col = go.AddComponent<CapsuleCollider>();
                col.height = height; col.radius = radius + 0.07f; col.center = new Vector3(0, height / 2, 0); // a little fatter than drawn: easier to click
                float bodyH = height - headD - 0.03f;
                Part(go.transform, "body", capsuleMesh, Mat(J.S(J.Get(p, "look"), "block")), new Vector3(0, bodyH / 2, 0), new Vector3(radius * 2, bodyH / 2, radius * 2));
                Part(go.transform, "head", sphereMesh, headMat, new Vector3(0, height - headD / 2, 0), Vector3.one * headD);
                var rec = go.AddComponent<PersonRecord>();
                rec.id = id; rec.personName = J.S(p, "name"); rec.where = where; rec.now = stop;
                var w = J.Get(p, "work");
                rec.job = w == null ? "no work (lives here)" : $"{J.S(w, "position") ?? J.S(w, "role")}, {(units.TryGetValue(J.S(w, "employer"), out var en) ? en : J.S(w, "employer"))} ({J.S(w, "post")}, shift {Clock((int)J.F(J.Get(w, "shift"), "from"))}–{Clock((int)J.F(J.Get(w, "shift"), "to"))})";
                var inst = J.S(p, "institution");
                var st = J.Get(p, "start");
                rec.temperament = $"{J.S(p, "temperament")}{(inst != null ? $" (to {inst})" : "")} · bravery {J.F(st, "bravery"):0.#}, resentment {J.F(st, "resentment"):0.#}, fear {J.F(st, "fear"):0.#}, loyalty {J.F(st, "loyalty"):0.#}";
                rec.need = J.S(p, "need");
                rec.ties = J.A(p, "ties").Select(t => $"{J.S(t, "kind")} · {(names.TryGetValue(J.S(t, "with"), out var n) ? $"{n} ({jobs[J.S(t, "with")]})" : J.S(t, "with"))}").ToArray();
                rec.routine = J.A(J.Get(p, "routine"), "stops").Select(s => $"{Clock((int)J.F(s, "from"))}–{Clock((int)J.F(s, "to"))}  {J.S(s, "doing")}, {(J.S(s, "place") == "beyond" ? "off the sheet" : places[J.S(s, "place")].name)}").ToArray();
                res.Drawn++;
            }
        }

        static void Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Mesh Prim(PrimitiveType t)
        {
            var g = GameObject.CreatePrimitive(t); var m = g.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(g);
            return m;
        }

        /// <summary>clock in [from, to), with to &lt; from running past midnight.</summary>
        static bool In(int clock, int from, int to) => from <= to ? clock >= from && clock < to : clock >= from || clock < to;
        static float Span(int a, int b) => ((b - a) % 1440 + 1440) % 1440;
        static string Sha(byte[] b) { using var h = SHA256.Create(); return string.Concat(h.ComputeHash(b).Select(x => x.ToString("x2"))); }

        /// <summary>Plan ground from the world export: the pavement height (plot slabs) and the footprints a person cannot stand in
        /// (buildings and civic masses at street level except overhead and frame pieces, landmarks, water), with `clear` metres round each.</summary>
        sealed class Ground
        {
            readonly List<(Vector2[] ring, Rect box)> blockers = new();
            readonly List<(Vector2 c, float r)> rounds = new();
            readonly List<(Vector2[] ring, Rect box, float top)> slabs = new();
            readonly float clear;

            public Ground(WorldDto w, HashSet<string> passable, float clear)
            {
                this.clear = clear;
                foreach (var m in w.Buildings.Concat(w.Civic))
                    if (m.Base < 2f && m.Height > 0.3f && !(m.Kit != null && passable.Contains(m.Kit.Id))) Add(m.Ring);
                foreach (var x in w.Ground.Water) Add(x.Ring);
                foreach (var l in w.Landmarks) rounds.Add((l.Centre, l.Radius));
                foreach (var p in w.Ground.Plots) if (p.Ring is { Length: >= 3 }) slabs.Add((p.Ring, Box(p.Ring, 0), p.Base + p.Height));
            }

            void Add(Vector2[] ring) { if (ring is { Length: >= 3 }) blockers.Add((ring, Box(ring, clear))); }

            static Rect Box(Vector2[] r, float pad)
            {
                float x0 = r.Min(v => v.x) - pad, y0 = r.Min(v => v.y) - pad;
                return new Rect(x0, y0, r.Max(v => v.x) + pad - x0, r.Max(v => v.y) + pad - y0);
            }

            public bool Blocked(Vector2 p)
            {
                foreach (var (c, r) in rounds) if ((p - c).magnitude < r + clear) return true;
                foreach (var (ring, box) in blockers)
                {
                    if (!box.Contains(p)) continue;
                    if (Poly.Contains(ring, p)) return true;
                    for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++) if (SegDist(p, ring[j], ring[i]) < clear) return true;
                }
                return false;
            }

            public float Height(Vector2 p)
            {
                float h = 0f;
                foreach (var (ring, box, top) in slabs) if (top < 1f && box.Contains(p) && Poly.Contains(ring, p)) h = Mathf.Max(h, top);
                return h;
            }

            static float SegDist(Vector2 p, Vector2 a, Vector2 b)
            {
                var ab = b - a; float t = ab.sqrMagnitude < 1e-9f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                return (p - (a + ab * t)).magnitude;
            }
        }
    }
}
