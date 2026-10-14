using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Suborned.Perf;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.Perf
{
    /// <summary>Resolves perf-stops.json against the built HarbourDistrict scene into the player's plans (PerfPlan): stand-offs by
    /// Rig.StandOff with the play camera's rotation and zoom (checked against the built camera), pan paths sampled every
    /// PathStep metres with their stand-offs, the passes and variants as written; the scene's fingerprint and export sha256.
    /// The sweep (Redline M8) frames a street grid plus every deck and concourse within the export's levels, after a warm-up,
    /// ranked by frame time; Refine frames a finer grid round the heaviest foci of a sweep's result.</summary>
    public static class PerfPlanner
    {
        const float PathStep = 2f;
        static string SpecPath => Path.Combine(Application.dataPath, "Suborned", "Editor", "World", "Perf", "perf-stops.json");
        static string ExportPath => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "world", "harbour-district.world.json");

        /// <summary>The named stops and pan paths with the spec's passes and variants.</summary>
        public static PerfPlan Stops()
        {
            var spec = Spec();
            var stops = new List<StopDto>(spec.stops);
            return Plan(spec, stops, spec.warmup, spec.passes, spec.variants);
        }

        /// <summary>The street grid and the levels' decks at the play zoom, each framed briefly after a warm-up.</summary>
        public static PerfPlan Sweep()
        {
            var spec = Spec(); var w = spec.sweep ?? throw new InvalidDataException("Perf: perf-stops.json has no sweep");
            var stops = new List<StopDto>();
            for (float z = w.z[0]; z <= w.z[1] + 1e-3f; z += w.step)
                for (float x = w.x[0]; x <= w.x[1] + 1e-3f; x += w.step)
                    stops.Add(new StopDto { name = $"sweep {x:0},0,{z:0}", why = "street grid", focus = new[] { x, 0f, z } });
            if (w.levelStep > 0f) stops.AddRange(LevelFoci(w.levelStep));
            return Plan(spec, stops, w.warmup, new[] { w.pass }, Array.Empty<PerfVariant>());
        }

        /// <summary>A grid `refine.step` apart within `refine.reach` of the `refine.top` foci with the highest frame p50 in a sweep result.</summary>
        public static PerfPlan Refine(string sweepResult)
        {
            var spec = Spec(); var w = spec.sweep ?? throw new InvalidDataException("Perf: perf-stops.json has no sweep");
            var run = JsonUtility.FromJson<PerfReport>(File.ReadAllText(sweepResult));
            if (run.exportSha256 != Stamp().exportSha256) throw new InvalidDataException($"Perf: {sweepResult} ran on export {run.exportSha256}, the scene is {Stamp().exportSha256}");
            var segs = new List<SegmentResult>(run.segments);
            segs.Sort((a, b) => b.frameMs.p50.CompareTo(a.frameMs.p50));
            var stops = new List<StopDto>(); var seen = new HashSet<Vector3Int>();
            for (int i = 0; i < Mathf.Min(w.refine.top, segs.Count); i++)
            {
                var c = segs[i].focus;
                for (float dz = -w.refine.reach; dz <= w.refine.reach + 1e-3f; dz += w.refine.step)
                    for (float dx = -w.refine.reach; dx <= w.refine.reach + 1e-3f; dx += w.refine.step)
                    {
                        var f = new Vector3(c.x + dx, c.y, c.z + dz);
                        if (seen.Add(Vector3Int.RoundToInt(f))) stops.Add(new StopDto { name = $"refine {f.x:0},{f.y:0.##},{f.z:0}", why = $"within {w.refine.reach} m of {segs[i].stop} ({segs[i].frameMs.p50} ms p50)", focus = new[] { f.x, f.y, f.z } });
                    }
            }
            return Plan(spec, stops, w.warmup, new[] { w.pass }, Array.Empty<PerfVariant>());
        }

        /// <summary>Grid points `step` apart inside each deck of each raised level of the export the scene was built from.</summary>
        static IEnumerable<StopDto> LevelFoci(float step)
        {
            var stamp = Stamp();
            var bytes = File.ReadAllBytes(ExportPath);
            using (var sha = SHA256.Create())
                if (BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() != stamp.exportSha256)
                    throw new InvalidDataException($"Perf: {ExportPath} is not the export the scene was built from ({stamp.exportSha256})");
            var world = WorldReader.Load(ExportPath);
            foreach (var lv in world.Levels)
            {
                if (lv.Height <= 0f) continue;
                foreach (var d in lv.Decks)
                {
                    Vector2 lo = d.Ring[0], hi = d.Ring[0];
                    foreach (var p in d.Ring) { lo = Vector2.Min(lo, p); hi = Vector2.Max(hi, p); }
                    for (float z = Mathf.Ceil(lo.y / step) * step; z <= hi.y; z += step)
                        for (float x = Mathf.Ceil(lo.x / step) * step; x <= hi.x; x += step)
                            if (Poly.Contains(d.Ring, new Vector2(x, z)))
                                yield return new StopDto { name = $"sweep {x:0},{lv.Height:0.##},{z:0}", why = $"{lv.Id} deck {d.Id}", focus = new[] { x, lv.Height, z } };
                }
            }
        }

        static PerfPlan Plan(SpecDto spec, List<StopDto> stops, PerfWarmup warmup, PerfPass[] passes, PerfVariant[] variants)
        {
            var (root, cam) = Scene();
            var ts = cam.GetComponent<TacticalShadows>(); var af = cam.GetComponent<ArchitectureFade>();
            var rot = cam.transform.rotation; float aspect = (float)spec.width / spec.height;
            float home = Rig.StandOff(rot, af.focus, cam.orthographicSize, aspect);
            if (Mathf.Abs(home - ts.focusDepth) > 0.01f)
                throw new InvalidOperationException($"Perf: Rig.StandOff at the play focus is {home:F3} m, the built play camera stands {ts.focusDepth:F3} m back: the stops would not use the real rig");
            var stamp = Stamp();
            var plan = new PerfPlan
            {
                fingerprint = Inspect.Fingerprint(root, GameObject.Find("batches").transform, GameObject.Find("cameras").transform),
                exportSha256 = stamp.exportSha256, source = stamp.source, scene = HarbourWorldBuilder.ScenePath, content = spec.content,
                pitch = rot.eulerAngles.x, yaw = rot.eulerAngles.y, aspect = aspect, farMargin = cam.farClipPlane - ts.focusDepth,
                width = spec.width, height = spec.height, warmup = warmup ?? new PerfWarmup(), passes = passes, variants = variants,
                stops = new PerfStop[stops.Count],
            };
            for (int i = 0; i < stops.Count; i++)
            {
                var s = stops[i];
                float z = s.zoom > 0f ? s.zoom : cam.orthographicSize;
                var stop = new PerfStop { name = s.name, why = s.why, zoom = z, speed = s.speed };
                if (s.path is { Length: > 0 })
                {
                    if (s.speed <= 0f || s.path.Length < 2) throw new InvalidDataException($"Perf: pan `{s.name}` needs two or more points and a speed");
                    stop.path = Sample(s.path, p => Rig.StandOff(rot, p, z, aspect));
                    stop.focus = stop.path[0].focus; stop.standOff = stop.path[0].standOff;
                }
                else { stop.focus = Vec(s.focus, s.name); stop.standOff = Rig.StandOff(rot, stop.focus, z, aspect); }
                plan.stops[i] = stop;
            }
            return plan;
        }

        /// <summary>A polyline (flattened [x, y, z] triples) sampled every PathStep metres, its corners kept.</summary>
        static PerfPathPoint[] Sample(PointDto[] points, Func<Vector3, float> standOff)
        {
            var list = new List<PerfPathPoint>();
            float along = 0f;
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vector3 a = Vec(points[i].at, "path"), b = Vec(points[i + 1].at, "path");
                float len = Vector3.Distance(a, b); int n = Mathf.Max(1, Mathf.CeilToInt(len / PathStep));
                for (int k = i == 0 ? 0 : 1; k <= n; k++)
                {
                    var p = Vector3.Lerp(a, b, (float)k / n);
                    list.Add(new PerfPathPoint { focus = p, standOff = standOff(p), along = along + len * k / n });
                }
                along += len;
            }
            return list.ToArray();
        }

        /// <summary>The built scene, open and saved (a build reads the file; the plan must describe the same scene).</summary>
        static (Transform root, Camera play) Scene()
        {
            if (Application.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Perf: not in Play Mode or while compiling");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != HarbourWorldBuilder.ScenePath) throw new InvalidOperationException($"Perf: open {HarbourWorldBuilder.ScenePath} first (active: {scene.path})");
            if (scene.isDirty) throw new InvalidOperationException("Perf: the scene has unsaved changes; rebuild or save it first");
            var root = GameObject.Find(HarbourWorldBuilder.RootName) ?? throw new InvalidOperationException("Perf: no world root");
            var play = GameObject.Find("cameras/play");
            if (!play || !play.TryGetComponent<Camera>(out var cam) || !play.GetComponent<TacticalShadows>() || !play.GetComponent<ArchitectureFade>())
                throw new InvalidOperationException("Perf: cameras/play is missing its Camera, TacticalShadows or ArchitectureFade");
            return (root.transform, cam);
        }

        static WorldStamp Stamp() =>
            GameObject.Find(HarbourWorldBuilder.RootName) is { } r && r.TryGetComponent<WorldStamp>(out var s) ? s : throw new InvalidOperationException("Perf: the world root has no WorldStamp");

        static SpecDto Spec()
        {
            var spec = JsonUtility.FromJson<SpecDto>(File.ReadAllText(SpecPath));
            if (spec.stops == null || spec.stops.Length == 0 || spec.width <= 0 || spec.height <= 0) throw new InvalidDataException($"Perf: {SpecPath} needs stops, width and height");
            return spec;
        }

        static Vector3 Vec(float[] a, string stop) => a is { Length: 3 } ? new Vector3(a[0], a[1], a[2]) : throw new InvalidDataException($"Perf: stop `{stop}` focus is not [x, y, z]");

        [Serializable]
        sealed class SpecDto
        {
            public string note, content;
            public int width, height;
            public PerfWarmup warmup;
            public PerfVariant[] variants;
            public PerfPass[] passes;
            public StopDto[] stops;
            public SweepDto sweep;
        }

        [Serializable]
        sealed class StopDto { public string name, why; public float[] focus; public float zoom, speed; public PointDto[] path; }

        [Serializable]
        sealed class PointDto { public float[] at; }

        [Serializable]
        sealed class SweepDto
        {
            public string note;
            public float step, levelStep;
            public float[] x, z;
            public PerfWarmup warmup;
            public PerfPass pass;
            public RefineDto refine;
        }

        [Serializable]
        sealed class RefineDto { public int top = 5; public float step = 10f, reach = 20f; }
    }
}
