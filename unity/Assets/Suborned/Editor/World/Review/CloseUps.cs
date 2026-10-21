using System.Collections.Generic;
using System.IO;
using System.Linq;
using Suborned.Robots;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Brief 026: the old city's close-up families shot on the new world, for side-by-side with briefs/026/old-decokit/ (same framing
    /// rule as the old capture: a 3/4 front view filling about 60% of the frame, from eye height or above). Pieces are picked nearest the
    /// Handover block (the capstone focus). Robots three times: neutral (t = 0), with an expression, and alarmed (red, angry, head on the
    /// lens). Writes &lt;out&gt;/close-&lt;family&gt;-&lt;piece&gt;.png, &lt;out&gt;/robots/&lt;piece&gt;-{neutral,mood,alarm}.png and closeups.txt (what was
    /// found, what is not placed). Observes only; the robots are put back to their rest pose. Menu: Suborned/Render/Close-ups (New Round).</summary>
    public static class CloseUps
    {
        const int W = 1920, H = 1080;
        static readonly Vector3 Block = new(-150f, 0f, -235f); // the Handover block's middle (world frame), where Splice's capstone programme stands

        static readonly (string family, string[] ids)[] Families =
        {
            ("robot", new[] { "RobotButler", "RobotNanny", "RobotPorter", "RobotShopkeeper", "RobotWaiter" }),
            ("vending", new[] { "VendingMachine_Cola", "VendingMachine_Snack", "Automat" }),
            ("signage", new[] { "Marquee_Cinema", "BladeSign", "Billboard_Pylon_A", "Billboard_Wall_A", "NeonStar_Wall", "AdColumn", "SandwichBoard", "InfoLightbox" }),
            ("plant", new[] { "CoconutPalm", "FanPalm", "Cypress", "FlowerBed", "Planter_Urn", "PottedTopiary", "TopiaryCone", "TubPalm" }),
            ("street", new[] { "SignalOrb", "Streetlamp", "Bench", "PhoneBooth", "BusShelter", "TicketMachine", "Hydrant", "StreetClock", "PneumaticPost", "DrainGrate", "ManholeCover", "BikeRack", "Tile_Tactile", "Tile_Terrazzo" }),
        };

        [MenuItem("Suborned/Render/Close-ups (New Round)")]
        static void Menu() => HarbourWorldBuilder.Batch(() => Run(Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "026", "rounds", "closeups-" + System.DateTime.Now.ToString("yyyyMMdd-HHmm"))));

        public static void Run(string outDir, string only = null)
        {
            if (!RenderSession.EnsureScene()) return;
            Directory.CreateDirectory(outDir); Directory.CreateDirectory(Path.Combine(outDir, "robots"));
            var root = GameObject.Find(HarbourWorldBuilder.RootName)?.transform ?? throw new System.InvalidOperationException("CloseUps: no HarbourDistrict root");
            var placed = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && t.gameObject.activeInHierarchy) placed.Add(t);
            var log = new System.Text.StringBuilder($"CloseUps from {placed.Count} placed kit instances, nearest the block at {Block}\n");
            var play = GameObject.Find("cameras/play");
            var go = new GameObject("closeups-026") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            if (play) { cam.CopyFrom(play.GetComponent<Camera>()); EditorUtility.CopySerialized(play.GetComponent<UniversalAdditionalCameraData>(), go.TryGetComponent<UniversalAdditionalCameraData>(out var d) ? d : go.AddComponent<UniversalAdditionalCameraData>()); }
            cam.enabled = false; cam.targetTexture = null; cam.orthographic = false; cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500f;
            try
            {
                using var session = new RenderSession();
                session.ShadowDistance = 80f;
                session.Grab(cam, 64, 64);
                foreach (var (family, ids) in Families)
                    foreach (var id in ids)
                    {
                        if (only != null && id != only) continue;
                        var t = placed.Where(p => Matches(p.name, id)).OrderBy(p => (p.position - Block).sqrMagnitude).FirstOrDefault();
                        if (!t) { log.AppendLine($"{family} {id}: not placed"); continue; }
                        Close(session, cam, t, Path.Combine(outDir, $"close-{family}-{id}.png"));
                        log.AppendLine($"{family} {id}: {t.name} at {t.position} ({Vector3.Distance(t.position, Block):0} m from the block)");
                        if (family == "robot" && t.GetComponent<RobotIdle>() is { } idle) Robot(session, cam, idle, Path.Combine(outDir, "robots", id), log);
                    }
                if (only == null || only == "Kerb") Kerb(session, cam, root, Path.Combine(outDir, "close-street-Kerb.png"), log);
            }
            finally { Object.DestroyImmediate(go); }
            File.WriteAllText(Path.Combine(outDir, only == null ? "closeups.txt" : $"closeups-{only}.txt"), log.ToString());
            Debug.Log($"CloseUps: -> {outDir}\n{log}");
        }

        static bool Matches(string name, string id)
        {
            var n = name.StartsWith("DK_") ? name.Substring(3) : name;
            return n == id || n.StartsWith(id + "_") || n.StartsWith(id + " ") || (id == "BladeSign" && n.StartsWith("BladeSign"));
        }

        static Bounds BoundsOf(Transform t)
        {
            var rs = KitLods.Lod0(t.gameObject).Where(r => r.enabled).ToArray();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.one);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void Save(RenderSession s, Camera cam, string file) => File.WriteAllBytes(file, s.Grab(cam, W, H).EncodeToPNG());

        static void Close(RenderSession s, Camera cam, Transform t, string file)
        {
            var b = BoundsOf(t);
            float size = Mathf.Max(b.size.y, Mathf.Max(b.size.x, b.size.z));
            cam.fieldOfView = 40f;
            float dist = size * .5f / Mathf.Tan(20f * Mathf.Deg2Rad) / .6f + .5f;
            var up = Vector3.up * Mathf.Max(0f, 1.6f - b.center.y + size * .1f);
            // the old framing (30° off the piece's front) unless a wall or another piece stands between: then the clearest of 12 headings
            Vector3 pos = default; int tried = 0;
            foreach (var turn in new[] { 30f, 0f, 60f, -30f, 90f, -60f, 120f, -90f, 150f, -120f, 180f, -150f })
            {
                var dir = Quaternion.Euler(0f, turn, 0f) * t.forward; dir.y = 0f; dir.Normalize();
                pos = b.center + dir * dist + up; tried++;
                if (Clear(pos, b, t)) break;
            }
            cam.transform.position = pos;
            cam.transform.LookAt(b.center);
            Save(s, cam, file);
            if (tried > 1) Debug.Log($"CloseUps: {Path.GetFileName(file)} framed on heading {tried} (the first ones were blocked)");
        }

        /// <summary>No other renderer's mesh between the lens and the piece (its centre and top): temporary mesh colliders on the renderers whose
        /// bounds meet the sight lines, removed again.</summary>
        static bool Clear(Vector3 from, Bounds b, Transform self)
        {
            var targets = new[] { b.center, new Vector3(b.center.x, b.max.y - 0.1f * b.size.y, b.center.z) };
            var added = new List<MeshCollider>();
            try
            {
                foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (!mr.enabled || mr.transform.IsChildOf(self) || mr.name.StartsWith("contact ")) continue;
                    var rb = mr.bounds; bool meets = false;
                    foreach (var to in targets) { var d = to - from; if (rb.IntersectRay(new Ray(from, d), out float hit) && hit < d.magnitude) meets = true; }
                    if (!meets || !mr.TryGetComponent<MeshFilter>(out var mf) || !mf.sharedMesh) continue;
                    var c = mr.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = mf.sharedMesh; added.Add(c);
                }
                Physics.SyncTransforms();
                foreach (var to in targets) { var d = to - from; if (Physics.Raycast(from, d.normalized, d.magnitude - 0.05f)) return false; }
                return true;
            }
            finally { foreach (var c in added) Object.DestroyImmediate(c); }
        }

        static void Robot(RenderSession s, Camera cam, RobotIdle idle, string stem, System.Text.StringBuilder log)
        {
            var b = BoundsOf(idle.transform);
            var fwd = idle.transform.forward; fwd.y = 0f; fwd.Normalize();
            var target = new Vector3(b.center.x, b.min.y + b.size.y * .72f, b.center.z);
            cam.fieldOfView = 30f;
            cam.transform.position = target + Quaternion.Euler(0f, 20f, 0f) * fwd * (b.size.y * 1.6f + .6f) + Vector3.up * .15f;
            cam.transform.LookAt(target);
            var face = idle.Face;
            float mood = face ? face.FirstMoodAt + .5f : 3f;
            idle.PoseAt(0f); Save(s, cam, stem + "-neutral.png");
            idle.PoseAt(mood); Save(s, cam, stem + "-mood.png");
            idle.Alarm(cam.transform, 100f); idle.PoseAt(100.8f); Save(s, cam, stem + "-alarm.png");
            idle.Calm(101f); idle.PoseAt(0f);
            idle.Alarm(null, float.PositiveInfinity); idle.Calm(float.PositiveInfinity); // back to its idle schedule
            log.AppendLine($"  robot faces: {face?.Screens} screens, {idle.Heads} head parts; mood at {mood:0.0} s");
        }

        /// <summary>The kerb and gutter at eye height: along a crossing's kerb (the signal orb nearest the block), looking down the street.</summary>
        static void Kerb(RenderSession s, Camera cam, Transform root, string file, System.Text.StringBuilder log)
        {
            var orb = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("DK_SignalOrb") || t.name.StartsWith("DK_SignalEye")).OrderBy(t => (t.position - Block).sqrMagnitude).FirstOrDefault();
            if (!orb) { log.AppendLine("kerb: no signal orb to stand at"); return; }
            var f = orb.forward; f.y = 0f; f.Normalize(); var side = Vector3.Cross(Vector3.up, f);
            cam.fieldOfView = 50f;
            cam.transform.position = orb.position + side * 3f - f * 1.5f + Vector3.up * (1.6f - orb.position.y + 0.18f);
            cam.transform.LookAt(orb.position - side * 6f + Vector3.up * 0.2f);
            Save(s, cam, file);
            log.AppendLine($"kerb: at the signal orb {orb.name} {orb.position}");
        }
    }
}
