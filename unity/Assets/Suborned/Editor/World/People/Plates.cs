using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Suborned.People;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.World.People
{
    /// <summary>Brief 011 C: background plates for Glyph's HUD concepts (direction A), named by place and city time, into
    /// &lt;repo&gt;/briefs/011/ui/plates/&lt;place&gt;-&lt;HHMM[SS]&gt;.png, with plates.json: each plate's camera, its time (city and
    /// mission) and the screen positions of the Handover's people (Inez, the door guard, the driver, Vale), so tags and cards can
    /// be anchored. The play camera (orthographic, zoom 14, pitch 38, yaw 30) moved to each plate's focus, its stand-off, shadow
    /// fit and architecture fade set as the play rig sets them; 1920x1080 through Capture (the honest path); the people layer
    /// (PeopleLayer) rebuilt at each plate's clock from out/people/&lt;plan&gt;.people.json. No IMGUI (the record panel, the
    /// `inside: N` tags) reaches a Camera.Render, so no debug UI is in a plate. The wide plate keeps pitch and yaw and zooms out to fit
    /// the civic block (its shadow bands scale with the zoom). A role M-2 cannot cast yet is reported with the people who stand at its
    /// mission point instead (`standingThere`); the M-2 line comes from plates/m2.txt (the checker's output, written beside the
    /// plates before the run). visiblePx counts the pixels a person changes when drawn alone over the plate without people (their
    /// shadow off): 0 = hidden behind architecture. Leaves the layer at 17:00 and saves the scene.
    /// Menu: Suborned/World/People/HUD Plates (011). Batchmode (needs a GPU): -executeMethod Suborned.World.People.Plates.Render</summary>
    public static class Plates
    {
        const int W = 1920, H = 1080;
        const float PlayZoom = 14f, Tall = 1.75f;
        static string Dir => Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "011", "ui", "plates");
        static string Repo(string rel) => Path.Combine(HarbourWorldBuilder.RepoRoot, rel);

        sealed class Plate
        {
            public string Name, Place, Clock, Focus, Note;
            public bool Wide;
            public Plate(string name, string place, string clock, string focus, string note, bool wide = false) { Name = name; Place = place; Clock = clock; Focus = focus; Note = note; Wide = wide; }
        }

        /// <summary>Focus: place:&lt;people place id&gt; | mission:&lt;mission destination name&gt; (block frame, to plan by plan civic.blockToPlan)
        /// | view:&lt;views.json view with a `look`&gt; | civic (the civic block's square and office rectangles, fitted).</summary>
        static readonly Plate[] All =
        {
            new("cafe-front", "the café front, Inez at the door", "17:24:30", "place:cafe-entrance", "mission 0:00"),
            new("cafe-front", "the café front, Inez at the door", "17:30", "place:cafe-entrance", "the handover chime"),
            new("office-door", "the Records Office public door", "17:26", "place:office-entrance", "the guards on the door"),
            new("office-door", "the Records Office public door", "17:30", "place:office-entrance", "the handover chime: the Office's shift ends, staff leave by this door"),
            new("official-bay", "the official bay on Apron Road", "17:30", "place:office-bay", "the drivers at the bay at the chime"),
            new("civic-block", "the whole civic block, wide", "17:28", "civic", "zoomed out from 14 to fit the block; pitch and yaw as play", wide: true),
            new("cafe-crossing", "the café crossing on Starline Street (brief 009's 009-cafe frame)", "17:24:30", "view:009-cafe", "mission 0:00"),
            new("service-lane", "the camera panned to the service lane (the Office's west side)", "17:30", "mission:Service lane", "the chime; Vale's exit if the driver is turned (5:50); the people layer does not play that branch"),
        };

        [MenuItem("Suborned/World/People/HUD Plates (011)")]
        public static void Render() => HarbourWorldBuilder.Batch(() =>
        {
            if (!Application.isBatchMode && !RenderSession.EnsureScene()) return;
            if (Application.isBatchMode) EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            Run();
        });

        /// <summary>Glint's light-honest done-check frame: cameras/play at its built focus, 1920x1080 through Capture, the people layer
        /// hidden (as Glint's -nopeople frames and the Warmth Probe), GPU Resident Drawer off. Written to out/latch-plates/; nothing is saved.</summary>
        public static string Probe()
        {
            var dir = Repo("out/latch-plates"); Directory.CreateDirectory(dir);
            var cam = GameObject.Find("cameras/play").GetComponent<Camera>();
            var hidden = GameObject.Find(PeopleLayer.RootName) is { } pl ? pl.GetComponentsInChildren<Renderer>().Where(r => !r.forceRenderingOff).ToArray() : new Renderer[0];
            try
            {
                foreach (var r in hidden) r.forceRenderingOff = true;
                using var session = new RenderSession();
                session.Grab(cam, 64, 64);
                using (cam.GetComponent<TacticalShadows>().Use())
                {
                    var file = Path.Combine(dir, "probe-play-nopeople.png");
                    File.WriteAllBytes(file, session.Grab(cam, W, H).EncodeToPNG());
                    return file;
                }
            }
            finally { foreach (var r in hidden) if (r) r.forceRenderingOff = false; }
        }

        public static string Run()
        {
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>() ?? throw new InvalidDataException("Plates: no WorldStamp; build the world first");
            var exportPath = PeopleLayer.ExportOf(stamp);
            var exportSha = Sha(File.ReadAllBytes(exportPath));
            if (stamp.exportSha256 != exportSha) throw new InvalidDataException($"Plates: the scene was built from export {stamp.exportSha256[..12]}, the export on disk is {exportSha[..12]}; rebuild the world first");
            var peopleBytes = File.ReadAllBytes(PeopleLayer.PeoplePath);
            var people = Json.Parse(new UTF8Encoding(false).GetString(peopleBytes).TrimStart('﻿'));
            var places = J.A(people, "places").ToDictionary(p => J.S(p, "id"), p => J.P(p, "at"));
            var persons = J.A(people, "persons").ToDictionary(p => J.S(p, "id"));
            var plan = Json.Parse(File.ReadAllText(Repo("contracts/world/harbour-district.plan.json")));
            var civic = J.Get(plan, "civic"); var b2p = J.Get(civic, "blockToPlan");
            Vector2 o = J.P(b2p, "origin"), ax = J.P(b2p, "x"), ay = J.P(b2p, "y");
            Vector2 ToPlan(Vector2 b) => o + b.x * ax + b.y * ay;
            var missionPath = Repo("contracts/missions/handover/mission.json");
            var mission = Json.Parse(File.ReadAllText(missionPath));
            var dest = J.A(mission, "destinations").ToDictionary(d => J.S(d, "name"), d => ToPlan(J.P(d, "at")));
            string startsAt = J.S(J.Get(mission, "clock"), "startsAt");
            float start = PeopleLayer.Minutes(startsAt);
            var views = J.A(Json.Parse(File.ReadAllText(HarbourWorldBuilder.ViewsPath)), "views");
            // the cast: rule M-2's, as Keel's site carries it (sim/data/handover.site.json cast, written by tools/missions/handover/cast.ts
            // from the promoted people); each role's mission point from mission.json roles[].at. Unknown role = reject.
            var sitePath = Repo("sim/data/handover.site.json");
            var siteCast = J.O(J.Get(Json.Parse(File.ReadAllText(sitePath)), "cast") ?? throw new InvalidDataException($"Plates: {sitePath} has no cast"));
            var castFrom = siteCast.TryGetValue("from", out var cf) ? cf as string : null;
            var castSha = siteCast.TryGetValue("sha256", out var cs) ? cs as string : null;
            var roleAt = J.A(mission, "roles").ToDictionary(r => J.S(r, "id"), r => J.S(r, "at"));
            var cast = new List<(string role, string id, string name, string temperament, string at)>();
            foreach (var role in new[] { "waitress", "guard", "driver", "councillor" })
            {
                if (!siteCast.TryGetValue(role, out var c) || J.S(c, "id") is not { Length: > 0 } cid) { cast.Add((role, null, null, null, roleAt.GetValueOrDefault(role))); continue; }
                if (!roleAt.ContainsKey(role)) throw new InvalidDataException($"Plates: the site casts {role}, mission.json has no such role (unknown = reject)");
                cast.Add((role, cid, J.S(c, "name"), J.S(c, "temperament"), roleAt[role]));
            }
            var m2File = Path.Combine(Dir, "m2.txt");
            string m2 = File.Exists(m2File) ? File.ReadAllText(m2File).Trim() : null;

            // the Handover's people as cast above; a role the site leaves uncast is reported by who stands at its mission point
            string Role(object p) => J.Get(p, "work") is { } w ? J.S(w, "role") ?? J.S(w, "position") : null;
            string Employer(object p) => J.Get(p, "work") is { } w ? J.S(w, "employer") : null;
            var guards = persons.Values.Where(p => Employer(p) == "records-office" && Role(p) == "guard").Select(p => J.S(p, "id")).ToHashSet();
            var drivers = persons.Values.Where(p => Employer(p) == "records-office" && Role(p) == "official-driver").Select(p => J.S(p, "id")).ToHashSet();

            Directory.CreateDirectory(Dir);
            var play = GameObject.Find("cameras/play")?.GetComponent<Camera>() ?? throw new FileNotFoundException("Plates: no cameras/play in the scene");
            var playTs = play.GetComponent<TacticalShadows>();
            var rot = play.transform.rotation;
            var clone = UnityEngine.Object.Instantiate(play.gameObject); clone.name = "plates-camera"; clone.hideFlags = HideFlags.HideAndDontSave;
            var cam = clone.GetComponent<Camera>(); cam.enabled = false;
            var ts = clone.GetComponent<TacticalShadows>(); var af = clone.GetComponent<ArchitectureFade>();
            var outPlates = new List<object>();
            var shot = new Dictionary<string, string>(); // png sha256 -> first plate file with those pixels
            var log = new StringBuilder();
            try
            {
                using var session = new RenderSession();
                bool warm = false;
                foreach (var plate in All)
                {
                    float clock = PeopleLayer.Minutes(plate.Clock);
                    // focus and zoom
                    Vector2 f2; float zoom = PlayZoom;
                    var kind = plate.Focus.Split(':')[0]; var key = plate.Focus.Contains(':') ? plate.Focus[(kind.Length + 1)..] : null;
                    switch (kind)
                    {
                        case "place": f2 = places.TryGetValue(key, out var pp) ? pp : throw new InvalidDataException($"Plates: no place {key} in the people file"); break;
                        case "mission": f2 = dest.TryGetValue(key, out var dp) ? dp : throw new InvalidDataException($"Plates: no destination {key} in {missionPath}"); break;
                        case "view":
                            var v = views.FirstOrDefault(x => J.S(x, "name") == key) ?? throw new InvalidDataException($"Plates: no view {key} in views.json");
                            var look = J.A(v, "look"); f2 = new Vector2((float)(double)look[0], (float)(double)look[1]); break;
                        case "civic":
                            var corners = J.O(J.Get(civic, "blocks")).Values.SelectMany(r =>
                            {
                                var a = J.A(r).Select(x => (float)(double)x).ToArray(); // [x0, x1, y0, y1], block frame
                                return new[] { new Vector2(a[0], a[2]), new Vector2(a[1], a[2]), new Vector2(a[1], a[3]), new Vector2(a[0], a[3]) }.Select(ToPlan);
                            }).ToArray();
                            f2 = new Vector2((corners.Min(c => c.x) + corners.Max(c => c.x)) / 2, (corners.Min(c => c.y) + corners.Max(c => c.y)) / 2);
                            var inv = Quaternion.Inverse(rot); float need = 0f;
                            foreach (var c in corners) { var l = inv * new Vector3(c.x - f2.x, 0f, c.y - f2.y); need = Mathf.Max(need, Mathf.Abs(l.y), Mathf.Abs(l.x) / ((float)W / H)); }
                            zoom = Mathf.Ceil(need * 1.06f); break;
                        default: throw new InvalidDataException($"Plates: focus `{plate.Focus}` (unknown = reject)");
                    }

                    var res = PeopleLayer.Snapshot(clock);
                    var focus = new Vector3(f2.x, 0f, f2.y);
                    cam.orthographicSize = zoom;
                    float stand = Rig.StandOff(rot, focus, zoom, (float)W / H);
                    clone.transform.SetPositionAndRotation(focus - rot * Vector3.forward * stand, rot);
                    cam.farClipPlane = Mathf.Max(play.farClipPlane, stand + 1600f);
                    float k = zoom / PlayZoom;
                    ts.focusDepth = stand; ts.nearBand = playTs.nearBand * k; ts.farBand = playTs.farBand * k;
                    if (af) af.focus = focus;

                    var root = GameObject.Find(PeopleLayer.RootName);
                    var recs = root.GetComponentsInChildren<PersonRecord>().ToDictionary(r => r.id);
                    var allPeople = root.GetComponentsInChildren<Renderer>();

                    var stem = $"{plate.Name}-{plate.Clock.Replace(":", "")}";
                    var file = Path.Combine(Dir, stem + ".png");
                    if (!warm) { session.Grab(cam, 64, 64); warm = true; } // the first render after opening a scene comes out flat
                    Color32[] Frame() { using (ts.Use()) return session.Grab(cam, W, H).GetPixels32(); }
                    string sameAs;
                    using (ts.Use())
                    {
                        var png = session.Grab(cam, W, H).EncodeToPNG();
                        File.WriteAllBytes(file, png);
                        var psha = Sha(png);
                        if (!shot.TryGetValue(psha, out sameAs)) { shot[psha] = stem + ".png"; sameAs = null; }
                    }

                    // people of interest, then their visible pixels: each drawn alone (no shadow) over the plate with no one drawn
                    Dictionary<string, object> Who(string id, string tag)
                    {
                        var p = persons[id]; var d = new Dictionary<string, object> { ["id"] = id, ["name"] = J.S(p, "name"), ["job"] = $"{Role(p)}, {Employer(p)}" };
                        if (tag != null) d["as"] = tag;
                        if (recs.TryGetValue(id, out var rec)) Place(d, rec);
                        else Absent(d, p, clock, places);
                        return d;
                    }
                    void Place(Dictionary<string, object> d, PersonRecord rec)
                    {
                        var at = rec.transform.position;
                        d["state"] = rec.where; d["drawn"] = true;
                        d["plan"] = R(new Vector2(at.x, at.z));
                        var feet = Px(cam, at); var head = Px(cam, at + Vector3.up * Tall);
                        d["feet"] = R(feet); d["head"] = R(head);
                        var b = rec.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                        var box = Box(cam, b);
                        d["box"] = new List<object> { Mathf.Floor(box.xMin), Mathf.Floor(box.yMin), Mathf.Ceil(box.xMax), Mathf.Ceil(box.yMax) };
                        d["inFrame"] = box.xMax >= 0 && box.xMin <= W && box.yMax >= 0 && box.yMin <= H;
                    }
                    void Absent(Dictionary<string, object> d, object p, float c, Dictionary<string, Vector2> pl)
                    {
                        // inside (exteriors only: not drawn) or off the sheet; anchor at the place's door point when it has one
                        var stops = J.A(J.Get(p, "routine"), "stops");
                        var s = stops.FirstOrDefault(x => InStop(c, (int)J.F(x, "from"), (int)J.F(x, "to")));
                        var place = s != null ? J.S(s, "place") : null;
                        d["drawn"] = false;
                        if (place == null || place == "beyond") { d["state"] = "off the sheet (not drawn)"; d["inFrame"] = false; return; }
                        d["state"] = $"{J.S(s, "doing")} inside, {place} (not drawn: exteriors only)";
                        var at = pl[place]; var door = Px(cam, new Vector3(at.x, 0f, at.y));
                        d["plan"] = R(at); d["door"] = R(door);
                        d["inFrame"] = door.x >= 0 && door.x <= W && door.y >= 0 && door.y <= H;
                    }

                    var castPeople = cast.Where(c => c.id != null).ToDictionary(c => c.role, c =>
                    {
                        if (!persons.ContainsKey(c.id)) throw new InvalidDataException($"Plates: the site casts {c.role} as {c.id}, who is not in {PeopleLayer.PeoplePath}; the people file and the site differ");
                        return Who(c.id, $"{c.name} ({c.role}, {c.temperament}; M-2 casts {c.id})");
                    });
                    List<Dictionary<string, object>> There(HashSet<string> pool, string point, float m) => point != null && dest.TryGetValue(point, out var at)
                        ? recs.Values.Where(r => pool.Contains(r.id) && (Flat(r.transform.position) - at).magnitude <= m).OrderBy(r => r.id, StringComparer.Ordinal).Select(r => Who(r.id, null)).ToList()
                        : new List<Dictionary<string, object>>();
                    var gThere = castPeople.ContainsKey("guard") ? new List<Dictionary<string, object>>() : There(guards, roleAt.GetValueOrDefault("guard"), 6f);
                    var dThere = castPeople.ContainsKey("driver") ? new List<Dictionary<string, object>>() : There(drivers, roleAt.GetValueOrDefault("driver"), 8f);

                    // visible pixels for everyone listed and in frame
                    var listed = castPeople.Values.Concat(gThere).Concat(dThere).Where(d => d["drawn"] is true && d["inFrame"] is true).ToList();
                    if (listed.Count > 0)
                    {
                        var was = allPeople.ToDictionary(r => r, r => (r.forceRenderingOff, r.shadowCastingMode));
                        try
                        {
                            foreach (var r in allPeople) r.forceRenderingOff = true;
                            var bare = Frame();
                            foreach (var d in listed)
                            {
                                var own = recs[(string)d["id"]].GetComponentsInChildren<Renderer>();
                                foreach (var r in own) { r.forceRenderingOff = false; r.shadowCastingMode = ShadowCastingMode.Off; }
                                var alone = Frame();
                                foreach (var r in own) { r.forceRenderingOff = true; r.shadowCastingMode = was[r].shadowCastingMode; }
                                var bx = (List<object>)d["box"];
                                d["visiblePx"] = Changed(bare, alone, (float)bx[0] - 2, (float)bx[1] - 2, (float)bx[2] + 2, (float)bx[3] + 2);
                            }
                        }
                        finally { foreach (var kv in was) { kv.Key.forceRenderingOff = kv.Value.forceRenderingOff; kv.Key.shadowCastingMode = kv.Value.shadowCastingMode; } }
                    }

                    var roles = new List<object>();
                    foreach (var c in cast)
                    {
                        var row = new Dictionary<string, object> { ["role"] = c.role, ["missionPoint"] = c.at ?? "inside the Records Office, then Meet driver by 6:10", ["cast"] = c.id };
                        if (c.id != null) row["person"] = castPeople[c.role];
                        else
                        {
                            row["why"] = $"the site casts no {c.role} (see casting.m2)";
                            var there = c.role == "guard" ? gThere : c.role == "driver" ? dThere : null;
                            if (there != null) { row["standingThere"] = there.Cast<object>().ToList(); row["standingThereNote"] = $"records-office {(c.role == "guard" ? "guards drawn within 6 m" : "official drivers drawn within 8 m")} of {c.at}"; }
                        }
                        roles.Add(row);
                    }
                    var camPos = clone.transform.position;
                    outPlates.Add(new Dictionary<string, object>
                    {
                        ["file"] = stem + ".png",
                        ["place"] = plate.Place,
                        ["clock"] = plate.Clock,
                        ["mission"] = MissionClock(clock - start),
                        ["what"] = plate.Note,
                        ["sameAs"] = sameAs, // the same pixels as an earlier plate (nothing in frame moved between their clocks), else null
                        ["camera"] = new Dictionary<string, object>
                        {
                            ["projection"] = "orthographic", ["zoom"] = zoom, ["pitch"] = R1(rot.eulerAngles.x), ["yaw"] = R1(rot.eulerAngles.y),
                            ["size"] = new List<object> { W, H }, ["focus"] = plate.Focus, ["focusPlan"] = R(f2),
                            ["position"] = new List<object> { R1(camPos.x), R1(camPos.y), R1(camPos.z) }, ["standOff"] = R1(stand),
                            ["shadowBands"] = new List<object> { R1(ts.nearBand), R1(ts.farBand) }, ["fadeFocus"] = R(f2),
                        },
                        ["people"] = new Dictionary<string, object> { ["drawn"] = res.Drawn, ["walking"] = res.Walking, ["inside"] = res.Inside, ["offSheet"] = res.Beyond },
                        ["roles"] = roles,
                    });
                    log.AppendLine($"{stem}.png: zoom {zoom}, focus {f2}, {res.Drawn} drawn ({res.Walking} walking), {string.Join(", ", castPeople.Select(kv => $"{kv.Key} {(kv.Value.TryGetValue("visiblePx", out var iv) ? iv : "-")} px"))}{(gThere.Count + dThere.Count > 0 ? $", {gThere.Count} guards and {dThere.Count} drivers at uncast points" : "")}");
                    Debug.Log($"Plates: {file} ({W}x{H}, zoom {zoom}, pitch 38, yaw 30, focus {f2}; {Capture.Describe})");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }

            var doc = new Dictionary<string, object>
            {
                ["schema"] = "suborned.plates/1",
                ["brief"] = "011 C: background plates for the HUD concepts, direction A (Glyph)",
                ["by"] = "Latch",
                ["made"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                ["how"] = $"Unity Editor, Suborned.World.People.Plates (menu Suborned/World/People/HUD Plates (011)); {Capture.Describe}; GPU Resident Drawer off for the renders; no debug UI (IMGUI never reaches a Camera.Render)",
                ["coords"] = "screen px at 1920x1080, origin top left, x right, y down; plan = metres in the plan frame (Unity x = plan x, Unity z = plan y)",
                ["scene"] = new Dictionary<string, object> { ["path"] = HarbourWorldBuilder.ScenePath, ["export"] = Rel(exportPath), ["exportSha256"] = exportSha, ["source"] = stamp.source, ["lightSha256"] = Sha(File.ReadAllBytes(HarbourWorldBuilder.LightPath)) },
                ["peopleLayer"] = new Dictionary<string, object> { ["file"] = Rel(PeopleLayer.PeoplePath), ["sha256"] = Sha(peopleBytes), ["inputs"] = J.Get(people, "inputs"),
                    ["model"] = "capsules in the job's colour block (people.json); people inside are not drawn (exteriors only); walkers on a straight line from the spot they stood on (the walk graph is not exported); background residents are counts, not drawn" },
                ["mission"] = new Dictionary<string, object> { ["file"] = Rel(missionPath), ["startsAt"] = startsAt, ["sha256"] = Sha(File.ReadAllBytes(missionPath)) },
                ["casting"] = new Dictionary<string, object>
                {
                    ["command"] = "bun tools/plan/cli.ts --overlay tools/missions/handover/overlay.ts",
                    ["m2"] = m2,
                    ["site"] = Rel(sitePath), ["from"] = castFrom, ["sha256"] = castSha,
                    ["cast"] = cast.Select(c => (object)new Dictionary<string, object> { ["role"] = c.role, ["id"] = c.id, ["name"] = c.name, ["temperament"] = c.temperament }).ToList(),
                    ["note"] = "Each role's person is the site's cast (rule M-2, written from the promoted people); a role the site leaves uncast names who stands at its mission point (standingThere).",
                },
                ["plates"] = outPlates,
            };
            File.WriteAllText(Path.Combine(Dir, "plates.json"), Write(doc) + "\n");
            PeopleLayer.Snapshot(17 * 60); // leave the layer as it was (17:00)
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new IOException("Plates: could not save the scene");
            Debug.Log($"Plates: {outPlates.Count} plates and plates.json in {Dir}\n{log}");
            return log.ToString();
        }

        static bool InStop(float c, int from, int to) => from <= to ? c >= from && c < to : c >= from || c < to;
        static Vector2 Flat(Vector3 v) => new(v.x, v.z);

        /// <summary>World point to screen px at W x H (origin top left), through the camera's projection at the plate's aspect.</summary>
        static Vector2 Px(Camera cam, Vector3 w)
        {
            cam.aspect = (float)W / H;
            var v = cam.WorldToViewportPoint(w);
            cam.ResetAspect();
            return new Vector2(v.x * W, (1f - v.y) * H);
        }

        static Rect Box(Camera cam, Bounds b)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var p = Px(cam, c);
                x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>Pixels inside the box (screen px, origin top left) where any channel differs by more than 3 levels.</summary>
        static int Changed(Color32[] a, Color32[] b, float x0, float y0, float x1, float y1)
        {
            int n = 0;
            int ix0 = Mathf.Clamp((int)x0, 0, W - 1), ix1 = Mathf.Clamp((int)x1, 0, W - 1), iy0 = Mathf.Clamp((int)y0, 0, H - 1), iy1 = Mathf.Clamp((int)y1, 0, H - 1);
            for (int y = iy0; y <= iy1; y++)
            {
                int row = (H - 1 - y) * W; // GetPixels32 rows run bottom up
                for (int x = ix0; x <= ix1; x++)
                {
                    var p = a[row + x]; var q = b[row + x];
                    if (Math.Abs(p.r - q.r) > 3 || Math.Abs(p.g - q.g) > 3 || Math.Abs(p.b - q.b) > 3) n++;
                }
            }
            return n;
        }

        static string MissionClock(float minutes)
        {
            int s = Mathf.RoundToInt(minutes * 60f);
            return s < 0 ? $"-{-s / 60}:{-s % 60:00}" : $"{s / 60}:{s % 60:00}";
        }

        static List<object> R(Vector2 v) => new() { R1(v.x), R1(v.y) };
        static double R1(float f) => Math.Round(f, 1);
        static string Rel(string path) => Path.GetRelativePath(HarbourWorldBuilder.RepoRoot, path).Replace('\\', '/');
        static string Sha(byte[] b) { using var h = SHA256.Create(); return string.Concat(h.ComputeHash(b).Select(x => x.ToString("x2"))); }

        /// <summary>Indented JSON for Dictionary / List / string / number / bool / null (the Json reader's shapes).</summary>
        static string Write(object v, int depth = 0)
        {
            string pad = new(' ', depth * 2), pad1 = new(' ', depth * 2 + 2);
            switch (v)
            {
                case null: return "null";
                case string s: return Quote(s);
                case bool b: return b ? "true" : "false";
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case float f: return ((double)f).ToString("R", CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case Dictionary<string, object> o:
                    if (o.Count == 0) return "{}";
                    return "{\n" + string.Join(",\n", o.Select(kv => $"{pad1}{Quote(kv.Key)}: {Write(kv.Value, depth + 1)}")) + $"\n{pad}}}";
                case List<object> a:
                    if (a.Count == 0) return "[]";
                    if (a.All(x => x is double or float or int)) return "[" + string.Join(", ", a.Select(x => Write(x))) + "]";
                    return "[\n" + string.Join(",\n", a.Select(x => pad1 + Write(x, depth + 1))) + $"\n{pad}]";
                default: throw new InvalidDataException($"Plates: cannot write {v.GetType()}");
            }
        }

        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s)
                sb.Append(c switch { '"' => "\\\"", '\\' => "\\\\", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", _ when c < 0x20 => $"\\u{(int)c:x4}", _ => c.ToString() });
            return sb.Append('"').ToString();
        }
    }
}
