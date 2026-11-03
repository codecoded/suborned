using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suborned.OldTown.Crowd
{
    /// <summary>The crowd test's readout and knobs (IMGUI, top left): frame rate and worst frame over the last half
    /// second, agents live and drawn, triangles submitted, the walk graph, and sliders for the agent count, the run
    /// share and time scale (slow it to check the feet). H hides it.</summary>
    public sealed class CrowdTestHud : MonoBehaviour
    {
        const float Window = .5f;

        [SerializeField] VatCrowd crowd;

        float acc, worst, fps, worstMs;
        int frames;
        bool show = true;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            acc += dt;
            worst = Mathf.Max(worst, dt);
            frames++;
            if (acc >= Window)
            {
                fps = frames / acc;
                worstMs = worst * 1000f;
                acc = worst = 0f;
                frames = 0;
            }
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) show = !show;
        }

        void OnGUI()
        {
            if (!show || crowd == null) return;
            float scale = Mathf.Max(1f, Screen.height / 1000f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(12f, 12f, 440f, 452f), GUI.skin.box);
            GUILayout.Label($"<b>Crowd test</b>  VAT instancing, {crowd.Citizens.Count} citizens", Rich);
            GUILayout.Label($"{fps:0} fps  ({1000f / Mathf.Max(fps, .01f):0.0} ms, worst {worstMs:0.0} ms)");
            GUILayout.Label($"agents {crowd.Agents:N0}, drawn {crowd.Drawn:N0}, {crowd.DrawnTris / 1e6f:0.00} M tris, LOD near/mid/far {crowd.DrawnPerLevel[0]:N0}/{crowd.DrawnPerLevel[1]:N0}/{crowd.DrawnPerLevel[2]:N0}");
            if (crowd.Graph != null) GUILayout.Label($"walk graph {crowd.Graph.nodes.Count:N0} nodes, {crowd.Graph.Links:N0} links");
            GUILayout.Label($"Agents {crowd.Agents:N0}");
            crowd.Agents = Mathf.RoundToInt(GUILayout.HorizontalSlider(crowd.Agents, 0, crowd.Capacity) / 50f) * 50;
            GUILayout.Label($"Running {crowd.RunShare:P0}");
            crowd.RunShare = GUILayout.HorizontalSlider(crowd.RunShare, 0f, 1f);
            GUILayout.Label($"Time scale {crowd.TimeScale:0.00}");
            crowd.TimeScale = GUILayout.HorizontalSlider(crowd.TimeScale, 0f, 2f);
            crowd.Shadows = GUILayout.Toggle(crowd.Shadows, " crowd casts shadows");
            crowd.ContactShadows = GUILayout.Toggle(crowd.ContactShadows, " contact shadows");
            crowd.Separate = GUILayout.Toggle(crowd.Separate, " separation (no walking through each other)");
            GUILayout.Label(string.Join("\n", crowd.Citizens.Select(c => $"  {c.name}: {c.mesh.vertexCount:N0} verts, " +
                                                                      string.Join(", ", c.clips.Select(k => $"{k.name} {k.Length:0.00} s @ {k.speed:0.0} m/s")))));
            GUILayout.Label("2.5D: WASD pan, wheel or +/- zoom, 1-4 closest/close/play/tactical, Q/E turn, Shift fast\n" +
                            "F: first person at street level (mouse look, Esc frees the cursor for the sliders), F again: back\nH hide");
            GUILayout.EndArea();
        }

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true };
    }
}
