using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static System.FormattableString;

namespace Suborned.Graybox
{
    /// <summary>Play-camera yaw study (brief 001): the play camera at play zoom (Rig.PlaySize, export view pitch) for every yaw
    /// in Yaws, centred on each of Centres, rendered to out/shots/study/ (stale PNGs cleared) with a contact sheet
    /// (index.html) and the numbers (visibility.json). Visibility is measured, not eyeballed: each mission point at
    /// EyeHeight above its ground casts a ray toward the camera (opposite the view direction, up to the camera plane) against
    /// MeshColliders added in memory to the Occluders layers; a point is visible when it is in frame and no ray hit occurs.
    /// Tower anchor posts, markers, objects and the cut-away twins are never occluders. Nothing is saved to the scene.
    /// Rays hit back faces for the study (Physics.queriesHitBackfaces, restored after): open shells such as the boarding
    /// pavilions have no bottom face, so a ray from under a canopy must hit its roof from below. At the plan yaw each
    /// point's blockers are compared with the plan checker's V-1 (V1.cs); any disagreement is logged as an error and
    /// fails the step after the outputs are written.
    /// Batchmode: -executeMethod Suborned.Graybox.Study.Render</summary>
    public static class Study
    {
        public static readonly int[] Yaws = { 0, 90, 180, 270 };
        const float EyeHeight = 1f, FarRay = 1000f;
        const int W = 1920, H = 1080;
        static readonly string[] Occluders =
            { "handover/buildings", "handover/structures", "plots", "landmarks", "skyRail", "pylons", "stations" };

        sealed class Point
        {
            public readonly string Name; public readonly Vector3 Eye; public readonly bool Door;
            public Point(string name, Vector3 eye, bool door) { Name = name; Eye = eye; Door = door; }
        }
        sealed class Centre
        {
            public readonly string Id, Label; public readonly Vector3 At;
            public Centre(string id, string label, Vector3 at) { Id = id; Label = label; At = at; }
        }
        sealed class Seen
        {
            public readonly Point P; public readonly Vector3 Viewport; public readonly bool InFrame; public readonly string[] Blockers;
            public Seen(Point p, Vector3 viewport, bool inFrame, string[] blockers) { P = p; Viewport = viewport; InFrame = inFrame; Blockers = blockers; }
            public bool Visible => InFrame && Blockers.Length == 0;
        }

        [MenuItem("Suborned/Graybox/Render Yaw Study")]
        public static void Render() => WorldBuilder.Batch(() =>
        {
            if (!WorldBuilder.MayReplaceScene()) return;
            ShaderUtil.allowAsyncCompilation = false;
            var world = WorldBuilder.Load(WorldBuilder.WorldPath);
            var h = J.Get(world, "handover");
            float pavement = J.F(J.O(world)["heights"], "pavement");
            var points = J.A(h, "points").Select(p =>
            {
                var at = J.P(p, "at");
                string name = J.S(p, "name");
                return new Point(name, new Vector3(at.x, HandoverLayer.Ground(h, at, pavement) + EyeHeight, at.y), HandoverLayer.IsDoor(name));
            }).ToList();
            var centres = Centres(h, pavement);
            var (planYaw, pitch) = Rig.View(world);
            int yawPlay = Mathf.RoundToInt(planYaw);
            if (!Yaws.Contains(yawPlay)) throw new InvalidDataException($"Graybox: plan yaw {planYaw} is not a study yaw");

            EditorSceneManager.OpenScene(WorldBuilder.ScenePath, OpenSceneMode.Single);
            string plan = WorldBuilder.ScenePlanHash();
            if (plan != WorldBuilder.PlanHash(world))
                throw new InvalidDataException($"Graybox: scene built from plan {plan}, export is {WorldBuilder.PlanHash(world)}; rebuild");
            var v1 = V1.Load(plan, planYaw, pitch);
            int colliders = AddColliders();
            var dir = Path.Combine(WorldBuilder.RepoRoot, "out", "shots", "study");
            Directory.CreateDirectory(dir);
            foreach (var stale in Directory.GetFiles(dir, "*.png")) File.Delete(stale);
            var play = Shots.Find("cameras/play");
            Rig.CutAway(play, false);
            Shots.Shot(play, 64, 64, null);

            var results = new Dictionary<(int, string), List<Seen>>();
            var anywhere = new Dictionary<int, List<Seen>>();
            bool backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            try
            {
                foreach (int yaw in Yaws)
                {
                    foreach (var c in centres)
                    {
                        Rig.Aim(play, c.At, yaw, pitch);
                        results[(yaw, c.Id)] = points.Select(p => Look(play, p, false)).ToList();
                        if (!anywhere.ContainsKey(yaw)) anywhere[yaw] = points.Select(p => Look(play, p, true)).ToList();
                        Shots.Shot(play, W, H, Path.Combine(dir, ShotName(c, yaw)), plan);
                    }
                }
            }
            finally { Physics.queriesHitBackfaces = backfaces; }
            int disagree = V1.Compare(yawPlay, anywhere[yawPlay].Select(s => (s.P.Name, s.Blockers)), v1, out var compare);
            File.WriteAllText(Path.Combine(dir, "index.html"), Sheet(points, centres, results, anywhere, colliders, plan, pitch)
                + "<h2>Study vs plan checker V-1 (plan yaw)</h2>\n<pre>" + Esc(compare) + "</pre>\n");
            File.WriteAllText(Path.Combine(dir, "visibility.json"), Numbers(centres, results, anywhere, plan, pitch));
            Debug.Log(Summary(centres, results, anywhere));
            Debug.Log(compare);
            if (disagree > 0) throw new InvalidDataException($"Graybox: study disagrees with V-1 at yaw {yawPlay} on {disagree} point(s)");
            Debug.Log($"Graybox: study at yaws {string.Join(", ", Yaws)} into {dir} ({colliders} occluder colliders, plan {plan})");
        });

        /// <summary>Café door, office north (public) entrance, official entrance, and the centre of each lay-by.</summary>
        static List<Centre> Centres(object h, float pavement)
        {
            Vector3 Point(string name)
            {
                var p = J.A(h, "points").FirstOrDefault(q => J.S(q, "name") == name)
                    ?? throw new InvalidDataException($"Graybox: study centre point '{name}' not in the export");
                var at = J.P(p, "at");
                return new Vector3(at.x, HandoverLayer.Ground(h, at, pavement), at.y);
            }
            Vector3 Bay(string id)
            {
                var b = J.A(h, "bays").FirstOrDefault(q => J.S(q, "id") == id)
                    ?? throw new InvalidDataException($"Graybox: study bay '{id}' not in the export");
                var poly = J.Poly(b, "polygon");
                var c = poly.Aggregate(Vector2.zero, (s, q) => s + q) / poly.Length;
                return new Vector3(c.x, J.F(b, "level"), c.y);
            }
            return new()
            {
                new("cafe-door", "Café door", Point("Café door")),
                new("office-north", "Office north entrance", Point("Office entrance")),
                new("office-official", "Office official entrance", Point("Official entrance")),
                new("getaway-bay", "Getaway bay", Bay("getawayBay")),
                new("official-bay", "Official bay", Bay("officialBay")),
            };
        }

        /// <summary>MeshColliders on every mesh under the occluder layers (in memory only).</summary>
        static int AddColliders()
        {
            int n = 0;
            var root = GameObject.Find(WorldBuilder.RootName).transform;
            foreach (var path in Occluders)
            {
                var layer = root.Find(path) ?? throw new InvalidDataException($"Graybox: occluder layer {path} not in the scene");
                foreach (var mf in layer.GetComponentsInChildren<MeshFilter>())
                {
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    n++;
                }
            }
            Physics.SyncTransforms();
            return n;
        }

        /// <summary>Where a point lands in the frame, and what lies between it and the camera. `far`: ignore the frame
        /// and the camera plane (occlusion only depends on the yaw for an orthographic camera).</summary>
        static Seen Look(Camera cam, Point p, bool far)
        {
            var vp = cam.WorldToViewportPoint(p.Eye);
            bool inFrame = vp.x >= 0 && vp.x <= 1 && vp.y >= 0 && vp.y <= 1 && vp.z > cam.nearClipPlane;
            float reach = far ? FarRay : vp.z - cam.nearClipPlane;
            var blockers = Physics.RaycastAll(p.Eye, -cam.transform.forward, reach)
                .Select(hit => Blocker(hit.collider.transform)).Distinct().OrderBy(s => s, System.StringComparer.Ordinal).ToArray();
            return new Seen(p, vp, inFrame, blockers);
        }

        /// <summary>Readable name of an occluder: plot blocks by plot id, everything else by its own name.</summary>
        static string Blocker(Transform t) =>
            t.parent && t.parent.parent && t.parent.parent.name == "plots" ? "plot " + t.parent.name : t.name;

        static string ShotName(Centre c, int yaw) => $"{c.Id}-yaw{yaw:D3}.png";

        static string Summary(List<Centre> centres, Dictionary<(int, string), List<Seen>> r, Dictionary<int, List<Seen>> any)
        {
            var sb = new StringBuilder("Graybox: yaw study (visible = in frame and unoccluded; of 21 points)\n");
            foreach (int yaw in Yaws)
            {
                sb.Append(Invariant($"yaw {yaw,3}: unoccluded {any[yaw].Count(s => s.Blockers.Length == 0)}/{any[yaw].Count}"));
                foreach (var c in centres)
                {
                    var s = r[(yaw, c.Id)];
                    sb.Append(Invariant($" | {c.Id} {s.Count(x => x.Visible)}/{s.Count(x => x.InFrame)}"));
                }
                sb.Append("\n  occluded: ").Append(string.Join("; ", any[yaw].Where(s => s.Blockers.Length > 0)
                    .Select(s => $"{s.P.Name} [{string.Join(", ", s.Blockers)}]"))).Append('\n');
            }
            return sb.ToString();
        }

        static string Numbers(List<Centre> centres, Dictionary<(int, string), List<Seen>> r, Dictionary<int, List<Seen>> any, string plan, float pitch)
        {
            static string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            var sb = new StringBuilder(Invariant($"{{\n \"planHash\": {Q(plan)}, \"eyeHeight\": {EyeHeight}, \"orthographicSize\": {Rig.PlaySize}, \"pitch\": {pitch},\n \"yaws\": [\n"));
            for (int i = 0; i < Yaws.Length; i++)
            {
                int yaw = Yaws[i];
                var occ = any[yaw].Where(s => s.Blockers.Length > 0);
                sb.Append(Invariant($"  {{\"yaw\": {yaw}, \"unoccluded\": {any[yaw].Count(s => s.Blockers.Length == 0)}, \"points\": {any[yaw].Count},\n"));
                sb.Append("   \"occluded\": {").Append(string.Join(", ", occ.Select(s => Q(s.P.Name) + ": [" + string.Join(", ", s.Blockers.Select(Q)) + "]"))).Append("},\n");
                sb.Append("   \"centres\": {").Append(string.Join(", ", centres.Select(c =>
                {
                    var s = r[(yaw, c.Id)];
                    return Invariant($"{Q(c.Id)}: {{\"inFrame\": {s.Count(x => x.InFrame)}, \"visible\": {s.Count(x => x.Visible)}, \"visibleNames\": [{string.Join(", ", s.Where(x => x.Visible).Select(x => Q(x.P.Name)))}]}}");
                }))).Append("}}").Append(i < Yaws.Length - 1 ? ",\n" : "\n");
            }
            return sb.Append(" ]\n}\n").ToString();
        }

        static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        static string Sheet(List<Point> points, List<Centre> centres, Dictionary<(int, string), List<Seen>> r,
            Dictionary<int, List<Seen>> any, int colliders, string plan, float pitch)
        {
            var sb = new StringBuilder();
            sb.Append("<!doctype html><meta charset=\"utf-8\"><title>Graybox yaw study</title>\n<style>")
              .Append("body{font:13px system-ui,sans-serif;margin:1em;color:#222}table{border-collapse:collapse;margin:.6em 0}")
              .Append("td,th{border:1px solid #ccc;padding:4px 6px;vertical-align:top}th{background:#f4f4f4}.n{text-align:right}")
              .Append(".shot{position:relative;width:460px}.shot img{width:100%;display:block}.shot svg{position:absolute;inset:0;width:100%;height:100%}")
              .Append(".best{background:#e6f6d8}small{color:#666}</style>\n")
              .Append(Invariant($"<h1>Graybox yaw study: play zoom (orthographic size {Rig.PlaySize}, pitch {pitch}°)</h1>\n<p>Plan <code>{Esc(plan)}</code> (hash stamped in the scene).</p>\n"))
              .Append(Invariant($"<p>Café 22 × 11.3 m × 34.8 m; Records Office at wing height 15.5 m (its two entrance towers are anchors only, drawn as thin dark posts to 38 m and never occluders). {points.Count} mission points; each tested at {EyeHeight} m above its ground by a ray toward the camera (back faces count) against {colliders} mesh colliders on: {string.Join(", ", Occluders)}. Plot block heights are the plan's (export `height`, on a block inset 3 m from the plot edge). "))
              .Append("Visible = in frame and unoccluded. For an orthographic camera occlusion depends only on the yaw, so 'unoccluded' is one number per yaw; the centres change only what is in frame.</p>\n")
              .Append("<p>Overlay: ring = mission point (green visible, red occluded), labelled squares = doors and entrances. Lime pegs and pink pads are the scene's own point and door markers; blue pegs are the crew.</p>\n");

            int best = Yaws.OrderByDescending(y => any[y].Count(s => s.Blockers.Length == 0)).First();
            sb.Append("<h2>Numbers</h2>\n<table><tr><th>yaw</th><th>looking</th><th>unoccluded (of 21)</th>");
            foreach (var c in centres) sb.Append($"<th>{Esc(c.Label)}<br><small>visible / in frame</small></th>");
            sb.Append("<th>doors visible (anywhere)</th><th>occluded points [by]</th></tr>\n");
            foreach (int yaw in Yaws)
            {
                var a = any[yaw];
                sb.Append($"<tr{(yaw == best ? " class=\"best\"" : "")}><th>{yaw}</th><td>{Looking(yaw)}</td><td class=\"n\">{a.Count(s => s.Blockers.Length == 0)}</td>");
                foreach (var c in centres)
                {
                    var s = r[(yaw, c.Id)];
                    sb.Append($"<td class=\"n\">{s.Count(x => x.Visible)} / {s.Count(x => x.InFrame)}</td>");
                }
                sb.Append("<td>").Append(Esc(string.Join(", ", a.Where(s => s.P.Door && s.Blockers.Length == 0).Select(s => s.P.Name)))).Append("</td><td>")
                  .Append(Esc(string.Join("; ", a.Where(s => s.Blockers.Length > 0).Select(s => $"{s.P.Name} [{string.Join(", ", s.Blockers)}]"))))
                  .Append("</td></tr>\n");
            }
            sb.Append("</table>\n<h2>Renders</h2>\n<table><tr><th></th>");
            foreach (int yaw in Yaws) sb.Append($"<th>yaw {yaw} ({Looking(yaw)})</th>");
            sb.Append("</tr>\n");
            foreach (var c in centres)
            {
                sb.Append(Invariant($"<tr><th>{Esc(c.Label)}<br>({c.At.x:0.#}, {c.At.z:0.#})</th>"));
                foreach (int yaw in Yaws)
                {
                    var s = r[(yaw, c.Id)];
                    string file = ShotName(c, yaw);
                    sb.Append($"<td><div class=\"shot\"><a href=\"{file}\"><img src=\"{file}\"></a>{Overlay(s)}</div>")
                      .Append($"{file} · visible {s.Count(x => x.Visible)} / in frame {s.Count(x => x.InFrame)}</td>");
                }
                sb.Append("</tr>\n");
            }
            return sb.Append("</table>\n").ToString();
        }

        static string Looking(int yaw) => yaw switch { 0 => "north", 90 => "east", 180 => "south", 270 => "west", _ => yaw + "°" };

        static string Overlay(List<Seen> seen)
        {
            var sb = new StringBuilder($"<svg viewBox=\"0 0 {W} {H}\" pointer-events=\"none\">");
            foreach (var s in seen.Where(x => x.InFrame))
            {
                float x = s.Viewport.x * W, y = (1 - s.Viewport.y) * H;
                string col = s.Blockers.Length == 0 ? "#1fb84a" : "#e0302a";
                if (s.P.Door)
                    sb.Append(Invariant($"<rect x=\"{x - 22:0}\" y=\"{y - 22:0}\" width=\"44\" height=\"44\" fill=\"none\" stroke=\"{col}\" stroke-width=\"6\"/>"))
                      .Append(Invariant($"<text x=\"{x + 28:0}\" y=\"{y - 26:0}\" font-size=\"34\" font-weight=\"bold\" fill=\"{col}\" stroke=\"#fff\" stroke-width=\"6\" paint-order=\"stroke\">{Esc(s.P.Name)}</text>"));
                else
                    sb.Append(Invariant($"<circle cx=\"{x:0}\" cy=\"{y:0}\" r=\"16\" fill=\"none\" stroke=\"{col}\" stroke-width=\"5\"/>"));
            }
            return sb.Append("</svg>").ToString();
        }
    }
}
