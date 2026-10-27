using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Brief 064 A3 items 1, 5, 7 (Latch, 2026-10-28): the no-look check, offscreen in batchmode. `-tag a3 -phase before|after`:
    /// the play camera at office-front y030 and y210 and cell-open y030 (ortho 14, pitch 38, Baseline072's poses) and one ground-view frame
    /// (perspective, the follow view's 55° at the office door, yaw 210), each at 1920 x 1080 through Capture → briefs/072/shots/a3/&lt;phase&gt;/,
    /// and a fight-lab frame: the fight lab's own signal lines and ghost on the signal layer, drawn as HostFightLab draws them, with a red, an
    /// amber and a violet cone on the default layer. `before` ran on the code as it was (SSAO on every camera, the depth texture forced, the
    /// signals overlay camera stacked). `after` runs on the A3 code and renders the fight-lab frame twice, on the cell's opening square at y030:
    /// through SignalsFeature (as shipped) and through the old path emulated in the same session (the layer back in PC_Renderer's filters in
    /// memory, the feature off, HostFightLab's overlay camera stacked with the base's aspect, as on screen), and diffs the two. A probe pass on
    /// the play camera records what each frame asked URP for (the camera's and post's depth requests, the SSAO feature's state). `after` diffs
    /// every frame against `before` (per channel, 8-bit sRGB) and, when a second `before` run is kept in before-run1/, the two `before` runs
    /// (the noise floor) → briefs/072/shots/a3/a3-after.json and diff-*.png (|Δ| × 16) where any pixel differs. Nothing is saved.</summary>
    public static partial class After072
    {
        sealed class MethodInfoHolder { readonly System.Reflection.MethodInfo m; public MethodInfoHolder(System.Reflection.MethodInfo m) { this.m = m; } public void Call() => m.Invoke(null, null); }
        static readonly string[] A3Frames = { "office-front-y030", "office-front-y210", "cell-open-y030", "ground-office-y210" };

        sealed class A3Probe : ScriptableRenderPass
        {
            public readonly List<string> Seen = new();
            public A3Probe() { renderPassEvent = RenderPassEvent.AfterRenderingTransparents; }
            /// <summary>The SSAO feature's state while this camera renders (with AfterOpaque the AO is blended into the colour, so URP never sets
            /// resourceData.ssaoTexture; the feature's isActive is what decides whether its pass is enqueued).</summary>
            static string Ssao()
            {
                foreach (var f in UniversalRenderPipeline.asset.rendererDataList[0].rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) return f.isActive ? "active" : "inactive";
                return "absent";
            }
            public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
            {
                var res = frameData.Get<UniversalResourceData>(); var cd = frameData.Get<UniversalCameraData>();
                Seen.Add($"{cd.camera.name} ({cd.renderType}): camera asks depth {cd.requiresDepthTexture}, post asks depth {cd.postProcessingRequiresDepthTexture}, ssaoFeature {Ssao()} (asks depth when active), asks opaque {cd.requiresOpaqueTexture}, postProcessing {cd.postProcessEnabled}");
            }
        }

        static void A3(Ctx c, RenderSession session, string root, string[] args)
        {
            int pi = Array.IndexOf(args, "-phase"); var phase = pi >= 0 && pi + 1 < args.Length ? args[pi + 1] : "after";
            if (phase != "before" && phase != "after") throw new InvalidOperationException("a3: -phase before|after");
            var top = Path.Combine(root, "briefs", "072", "shots", "a3"); var dir = Path.Combine(top, phase); Directory.CreateDirectory(dir);
            var urp = UniversalRenderPipeline.asset;
            var probe = new A3Probe(); var notes = new List<string>();
            string frame = null;
            void Begin(ScriptableRenderContext _, Camera cam)
            {
                if (cam != c.cam && cam.name != "signals camera") return;
                probe.Seen.Add($"[{frame}]");
                cam.GetUniversalAdditionalCameraData()?.scriptableRenderer?.EnqueuePass(probe);
            }
            RenderPipelineManager.beginCameraRendering += Begin;
            var host = UnityEngine.Object.FindAnyObjectByType<global::Suborned.Play.HandoverHost>(FindObjectsInactive.Include);
            int maskWas = c.cam.cullingMask;
            try
            {
                notes.Add($"asset: requireDepthTexture {urp.supportsCameraDepthTexture}, requireOpaqueTexture {urp.supportsCameraOpaqueTexture}, msaa {urp.msaaSampleCount}, renderScale {urp.renderScale}, hdr {urp.supportsHDR}");
                var ts = c.shadows; var orthoSsao = ts ? typeof(TacticalShadows).GetField("orthoSsao") : null;
                notes.Add($"TacticalShadows.orthoSsao: {(orthoSsao == null ? "absent (before A3: SSAO on every camera)" : orthoSsao.GetValue(ts))}");
                void Shoot(string name)
                {
                    frame = name;
                    session.Grab(c.cam, W, H); // settle (TAA-free, but the first render of a pose can differ: the fade's tiles, shadows)
                    File.WriteAllBytes(Path.Combine(dir, name + ".png"), session.Grab(c.cam, W, H).EncodeToPNG());
                }
                foreach (var (id, f, yaw) in new[] { ("office-front", Baseline072.OfficeFront, 30f), ("office-front", Baseline072.OfficeFront, 210f), ("cell-open", c.views.First(v => v.Id == "cell-open").Focus, 30f) })
                {
                    Baseline072.Pose(c.cam, c.shadows, c.fade, f, yaw, c.standOff);
                    Shoot($"{id}-y{yaw:000}");
                }
                // the ground view: the follow view's perspective (HostFollow.followFov 55) at the Records Office door, played at 210
                {
                    var focus = new Vector3(Baseline072.OfficeFront.x, 0f, Baseline072.OfficeFront.y);
                    c.cam.orthographic = false; c.cam.fieldOfView = host ? host.followFov : 55f; c.cam.nearClipPlane = 0.2f; c.cam.farClipPlane = 900f;
                    c.cam.transform.rotation = Quaternion.Euler(12f, 210f, 0f);
                    c.cam.transform.position = focus + Vector3.up * 1.5f - c.cam.transform.forward * 7f;
                    if (c.fade) c.fade.focus = focus;
                    Shoot("ground-office-y210");
                }
                // the fight-lab frame: signals as HostFightLab draws them, cones on the default layer
                // (on the cell's opening square, open paving at y030: the office front's street sits under the Records Office mass at y030)
                var cellFocus = c.views.First(v => v.Id == "cell-open").Focus;
                Baseline072.Pose(c.cam, c.shadows, c.fade, cellFocus, 30f, c.standOff);
                var made = new List<UnityEngine.Object>();
                try
                {
                    SignalFrame(c.cam, host, made, notes, cellFocus);
                    if (phase == "before") FightLab("fightlab-cell-y030", true);
                    else
                    {
                        FightLab("fightlab-cell-y030", false);
                        // the old path emulated in this session, for a like-for-like diff: the signal layer back in PC_Renderer's scene filters (in
                        // memory, put back below, never saved), SignalsFeature off, HostFightLab's overlay camera stacked
                        var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
                        var feat = data.rendererFeatures.FirstOrDefault(x => x && x.GetType().Name == "SignalsFeature");
                        LayerMask pre = data.prepassLayerMask, opq = data.opaqueLayerMask, trn = data.transparentLayerMask;
                        try
                        {
                            data.prepassLayerMask = ~0; data.opaqueLayerMask = ~0; data.transparentLayerMask = ~0; if (feat) feat.SetActive(false);
                            notes.Add($"fight-lab overlay emulation: PC_Renderer filters -1 (were {opq.value}), SignalsFeature {(feat ? "off" : "absent")}");
                            FightLab("fightlab-cell-y030-overlay", true);
                        }
                        finally { data.prepassLayerMask = pre; data.opaqueLayerMask = opq; data.transparentLayerMask = trn; if (feat) feat.SetActive(true); }
                    }
                }
                finally
                {
                    foreach (var o in made) if (o) UnityEngine.Object.DestroyImmediate(o);
                    c.cam.cullingMask = maskWas;
                }

                void FightLab(string name, bool overlayPath)
                {
                    const int SignalLayer = 8; GameObject overlay = null; MethodInfoHolder want = null;
                    try
                    {
                        if (overlayPath)
                        {   // HostFightLab.SignalCamera as it was (0.2.142): an overlay camera, no post, stacked; the play camera leaves the layer out.
                            // On screen both cameras take the screen's aspect; offscreen the overlay has no target of its own, so it is given the base's
                            overlay = new GameObject("signals camera"); overlay.transform.SetParent(c.cam.transform, false);
                            var sc = overlay.AddComponent<Camera>(); sc.cullingMask = 1 << SignalLayer; sc.clearFlags = CameraClearFlags.Nothing;
                            var d = overlay.AddComponent<UniversalAdditionalCameraData>();
                            d.renderType = CameraRenderType.Overlay; d.renderPostProcessing = false; d.renderShadows = false; d.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                            c.cam.GetUniversalAdditionalCameraData().cameraStack.Add(sc);
                            c.cam.cullingMask = maskWas & ~(1 << SignalLayer);
                            sc.orthographic = c.cam.orthographic; sc.orthographicSize = c.cam.orthographicSize; sc.fieldOfView = c.cam.fieldOfView; sc.nearClipPlane = c.cam.nearClipPlane; sc.farClipPlane = c.cam.farClipPlane;
                            sc.aspect = (float)W / H;
                        }
                        else
                        {   // A3 item 7: the play camera draws the layer itself, after post, when asked (HostFightLab asks each frame it draws a signal)
                            c.cam.cullingMask = maskWas | (1 << SignalLayer);
                            want = new MethodInfoHolder(Type.GetType("Suborned.SignalsFeature, Suborned.Runtime")?.GetMethod("Want") ?? throw new InvalidOperationException("a3 after: no Suborned.SignalsFeature.Want"));
                        }
                        frame = name;
                        want?.Call(); session.Grab(c.cam, W, H);
                        want?.Call(); File.WriteAllBytes(Path.Combine(dir, name + ".png"), session.Grab(c.cam, W, H).EncodeToPNG());
                    }
                    finally
                    {
                        if (overlay) { c.cam.GetUniversalAdditionalCameraData().cameraStack.Remove(overlay.GetComponent<Camera>()); UnityEngine.Object.DestroyImmediate(overlay); }
                        c.cam.cullingMask = maskWas;
                    }
                }
            }
            finally { RenderPipelineManager.beginCameraRendering -= Begin; }

            var sb = new StringBuilder();
            sb.Append("{\n  \"schema\": \"suborned.064.a3/1\",\n");
            sb.Append($"  \"phase\": \"{phase}\",\n  \"when\": \"{DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}\",\n");
            var stamp = UnityEngine.Object.FindAnyObjectByType<WorldStamp>();
            sb.Append($"  \"export\": \"{Esc(stamp ? $"{stamp.exportPath} sha256 {stamp.exportSha256}" : "no WorldStamp")}\",\n");
            sb.Append($"  \"method\": \"{Esc(Capture.Describe)}; 1920x1080; every frame rendered twice, the second kept\",\n");
            sb.Append("  \"notes\": [\n" + string.Join(",\n", notes.Select(n => $"    \"{Esc(n)}\"")) + "\n  ],\n");
            sb.Append("  \"urpFrames\": [\n" + string.Join(",\n", probe.Seen.Select(n => $"    \"{Esc(n)}\"")) + "\n  ]");
            if (phase == "after" && Directory.Exists(Path.Combine(top, "before-run1"))) sb.Append(",\n  \"noiseBeforeRun1VsBefore\": [\n" + string.Join(",\n", A3Frames.Select(f => Diff(Path.Combine(top, "before-run1", f + ".png"), Path.Combine(top, "before", f + ".png"), Path.Combine(top, "noise-" + f + ".png")))) + "\n  ]");
            if (phase == "after") sb.Append(",\n  \"diff\": [\n" + string.Join(",\n", A3Frames.Select(f => Diff(Path.Combine(top, "before", f + ".png"), Path.Combine(dir, f + ".png"), Path.Combine(top, "diff-" + f + ".png")))) + "\n  ]");
            if (phase == "after") sb.Append(",\n  \"signalsFeatureVsOverlayEmulated\": [\n" + Diff(Path.Combine(dir, "fightlab-cell-y030-overlay.png"), Path.Combine(dir, "fightlab-cell-y030.png"), Path.Combine(top, "diff-fightlab-feature-vs-overlay.png")) + "\n  ]");
            sb.Append("\n}\n");
            File.WriteAllText(Path.Combine(top, $"a3-{phase}.json"), sb.ToString());
            Debug.Log($"064 a3 ({phase}): {A3Frames.Length} frames → {dir}\n{sb}");
        }

        /// <summary>Per-channel 8-bit difference of two PNGs: pixels that differ at all, by more than 2 and 8 levels, the largest step, and the
        /// changed pixels' bounding box; an amplified |Δ| image when any pixel differs.</summary>
        static string Diff(string a, string b, string outPng)
        {
            var name = Path.GetFileNameWithoutExtension(b);
            if (!File.Exists(a) || !File.Exists(b)) return $"    {{ \"frame\": \"{name}\", \"missing\": \"{Esc(File.Exists(a) ? b : a)}\" }}";
            var ta = new Texture2D(2, 2); ta.LoadImage(File.ReadAllBytes(a)); var tb = new Texture2D(2, 2); tb.LoadImage(File.ReadAllBytes(b));
            if (ta.width != tb.width || ta.height != tb.height) return $"    {{ \"frame\": \"{name}\", \"sizeMismatch\": \"{ta.width}x{ta.height} vs {tb.width}x{tb.height}\" }}";
            var pa = ta.GetPixels32(); var pb = tb.GetPixels32(); int any = 0, gt2 = 0, gt8 = 0, max = 0; long sum = 0;
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1; var d = new Color32[pa.Length];
            for (int i = 0; i < pa.Length; i++)
            {
                int m = Mathf.Max(Mathf.Abs(pa[i].r - pb[i].r), Mathf.Max(Mathf.Abs(pa[i].g - pb[i].g), Mathf.Abs(pa[i].b - pb[i].b)));
                byte v = (byte)Mathf.Min(255, m * 16); d[i] = new Color32(v, v, v, 255);
                if (m == 0) continue;
                any++; if (m > 2) gt2++; if (m > 8) gt8++; if (m > max) max = m; sum += m;
                int x = i % ta.width, y = i / ta.width; x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            if (any > 0) { var td = new Texture2D(ta.width, ta.height, TextureFormat.RGB24, false); td.SetPixels32(d); td.Apply(); File.WriteAllBytes(outPng, td.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(td); }
            else if (File.Exists(outPng)) File.Delete(outPng);
            int ht = ta.height; UnityEngine.Object.DestroyImmediate(ta); UnityEngine.Object.DestroyImmediate(tb);
            string box = any > 0 ? $", \"box\": [{x0}, {ht - 1 - y1}, {x1}, {ht - 1 - y0}]" : "";
            return $"    {{ \"frame\": \"{name}\", \"pixels\": {pa.Length}, \"differ\": {any}, \"over2\": {gt2}, \"over8\": {gt8}, \"maxStep\": {max}, \"meanStepOfChanged\": {(any > 0 ? F((double)sum / any, 2) : "0")}{box} }}";
        }

        /// <summary>The fight lab's frozen frame, rebuilt from HostFightLab's recipe (SigMat, Line, DrawLine, Ghost, GhostMat): our turquoise
        /// dashed line to a stop point and on to the target with a ghost standing there; a rival's violet and the guard's amber dotted line with
        /// their 8 px heads; plus a red, an amber and a violet cone (the fan template) on the default layer, which the main pass draws.</summary>
        static void SignalFrame(Camera cam, global::Suborned.Play.HandoverHost host, List<UnityEngine.Object> made, List<string> notes, Vector2 at)
        {
            const int SignalLayer = 8;
            var unlit = host && host.unlitTemplate ? host.unlitTemplate : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var fanTpl = host && host.fanTemplate ? host.fanTemplate : unlit;
            notes.Add($"fight-lab frame: unlit template {unlit.name} ({unlit.shader.name}), fan template {fanTpl.name} ({fanTpl.shader.name})");
            Color turq = global::Suborned.Play.HandoverHost.CellTurquoise, violet = global::Suborned.Play.HandoverHost.RivalViolet, amber = global::Suborned.Play.HandoverHost.PoliceAmber;
            Texture2D Pattern(int on, int off) { var t = new Texture2D(on + off, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point }; var px = new Color32[on + off]; for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(i < on ? 255 : 0)); t.SetPixels32(px); t.Apply(); made.Add(t); return t; }
            var dash = Pattern(6, 4); var dot = Pattern(2, 5);
            Material Sig(Color col, Texture2D pattern)
            {
                var m = new Material(unlit) { name = "signal" }; m.SetColor("_BaseColor", col); m.renderQueue = 3100;
                if (pattern) { m.SetTexture("_BaseMap", pattern); m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); }
                made.Add(m); return m;
            }
            var ours = Sig(turq, dash); var rival = Sig(violet, dot); var police = Sig(amber, dot); var headR = Sig(violet, null); var headP = Sig(amber, null);
            float mpp = 2f * cam.orthographicSize / 1080f;
            LineRenderer Make(string n)
            {
                var go = new GameObject(n) { layer = SignalLayer }; made.Add(go);
                var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.positionCount = 2; lr.alignment = LineAlignment.View; lr.numCapVertices = 0;
                lr.textureMode = LineTextureMode.Tile; lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; return lr;
            }
            var chest = Vector3.up * 1.2f;
            void Line(Vector3 a, Vector3 b, bool our, Color side)
            {
                var dir = b - a; dir.y = 0f; dir.Normalize(); var right = Vector3.Cross(Vector3.up, dir) * (4f * mpp); a += right; b += right;
                float head = 8f * mpp; var body = Make("signal-line"); body.widthMultiplier = 2f * mpp;
                var mat = our ? ours : side == violet ? rival : police; body.sharedMaterial = mat;
                var end = our ? b : b - dir * head; body.SetPosition(0, a); body.SetPosition(1, end);
                mat.SetTextureScale("_BaseMap", new Vector2(1f / ((our ? 10f : 7f) * mpp), 1f));
                if (our) return;
                mat.SetTextureOffset("_BaseMap", new Vector2(-0.35f, 0f)); // a fixed march phase (the game's is Time.unscaledTime × 1.2)
                var hd = Make("signal-head"); hd.sharedMaterial = side == violet ? headR : headP; hd.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)); hd.widthMultiplier = head;
                hd.SetPosition(0, end); hd.SetPosition(1, b);
            }
            var o = new Vector3(at.x, 0f, at.y);
            Vector3 agent = o + new Vector3(-6f, 0f, 5f), stop = o + new Vector3(-1.5f, 0f, 1f), target = o + new Vector3(5f, 0f, -4f), rivalAt = o + new Vector3(9f, 0f, 5f), guardAt = o + new Vector3(-8f, 0f, -6f);
            Line(agent + chest, stop + chest, true, turq); Line(stop + chest, target + chest, true, turq);
            Line(rivalAt + chest, agent + chest, false, violet); Line(guardAt + chest, agent + chest, false, amber);
            // the ghost at the stop point: a capsule (HostFightLab's fallback body) in the ghost material, over a dashed ring
            var ghostMat = new Material(fanTpl) { name = "signal-ghost" }; ghostMat.SetColor("_BaseColor", new Color(turq.r, turq.g, turq.b, 0.55f)); if (ghostMat.HasProperty("_ZWrite")) ghostMat.SetFloat("_ZWrite", 1f); made.Add(ghostMat);
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule); made.Add(cap); UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.name = "signal-ghost"; cap.layer = SignalLayer; cap.transform.position = stop + Vector3.up * 0.9f; cap.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            var cmr = cap.GetComponent<MeshRenderer>(); cmr.sharedMaterial = ghostMat; cmr.shadowCastingMode = ShadowCastingMode.Off; cmr.receiveShadows = false;
            var ring = Make("signal-ghost-ring"); ring.loop = true; ring.positionCount = 24; ring.sharedMaterial = ours; ring.widthMultiplier = 2f * mpp;
            for (int j = 0; j < 24; j++) { float t = j / 24f * Mathf.PI * 2f; ring.SetPosition(j, stop + new Vector3(Mathf.Cos(t) * 0.45f, 0.05f, Mathf.Sin(t) * 0.45f)); }
            // cones (the main pass; signals red, guard amber, rivals violet): flat 60° fans 6 m long, 35 cm up
            void Cone(Vector3 at, float yaw, Color col)
            {
                var mesh = new Mesh { name = "a3-cone" }; made.Add(mesh); const int seg = 16; var v = new Vector3[seg + 2]; var tri = new int[seg * 3];
                for (int i = 0; i <= seg; i++) { float an = (yaw - 30f + 60f * i / seg) * Mathf.Deg2Rad; v[i + 1] = new Vector3(Mathf.Sin(an), 0f, Mathf.Cos(an)) * 6f; }
                for (int i = 0; i < seg; i++) { tri[i * 3] = 0; tri[i * 3 + 1] = i + 1; tri[i * 3 + 2] = i + 2; }
                mesh.vertices = v; mesh.triangles = tri; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var go = new GameObject("a3-cone"); made.Add(go); go.transform.position = at + Vector3.up * 0.35f; // above the square's paving (raised over the street datum)
                go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
                var m = new Material(fanTpl) { name = "a3-cone" }; m.SetColor("_BaseColor", new Color(col.r, col.g, col.b, 0.45f)); made.Add(m); mr.sharedMaterial = m;
            }
            Cone(o + new Vector3(2f, 0f, 6f), 200f, new Color(0.93f, 0.16f, 0.16f)); Cone(guardAt, 60f, amber); Cone(rivalAt, 250f, violet);
        }
    }
}
