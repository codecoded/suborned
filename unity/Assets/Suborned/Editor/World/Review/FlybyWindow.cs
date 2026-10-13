using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Suborned.World.Review
{
    /// <summary>Suborned/World/Flyby: watch the flyby live. Drives a throwaway camera (not saved, drawn over the scene's play
    /// camera in the Game view) along flyby.json, in Edit or Play mode; optionally moves the Scene view with it. Play/pause
    /// (Space with the window focused), speed, scrub, jump to a stop, loop. The URP shadow distance follows the path while it
    /// runs and is restored on Stop or when the window closes.</summary>
    public sealed class FlybyWindow : EditorWindow
    {
        FlybyPath path;
        Camera cam;
        float t, speed = 1f, shadowRestore = -1f;
        bool playing, loop = true, sceneView, shadows = true, live;
        double last;
        string error;

        [MenuItem("Suborned/World/Flyby")]
        static void Open()
        {
            var w = GetWindow<FlybyWindow>("Flyby");
            w.minSize = new Vector2(360, 260);
            w.Load();
            w.playing = w.path != null;
        }

        void Load()
        {
            error = null;
            try
            {
                if (!RenderSession.EnsureScene()) { error = "Open Assets/Scenes/HarbourDistrict.unity first."; return; }
                path = Flyby.Load();
                t = Mathf.Clamp(t, 0f, path.Duration);
                Ensure();
                ApplyPose();
            }
            catch (Exception e) { error = e.Message; path = null; Debug.LogException(e); }
        }

        const string CamName = "flyby-camera";

        void Ensure()
        {
            if (cam || !live) return;
            cam = Stray();
            if (cam) return;
            cam = RenderSession.TempCamera(CamName);
            cam.depth = 100; // above the scene's play camera in the Game view
        }

        void OnEnable() { live = true; last = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; AssemblyReloadEvents.beforeAssemblyReload += Release; EditorApplication.playModeStateChanged += ModeChanged; }
        // `live` guards against a Tick already queued in the current update pass (Close() called from inside an update) re-creating the camera.
        void OnDisable() { live = false; EditorApplication.update -= Tick; AssemblyReloadEvents.beforeAssemblyReload -= Release; EditorApplication.playModeStateChanged -= ModeChanged; Release(); }

        /// <summary>Play mode with or without scene reload: reuse a surviving camera, else make one.</summary>
        void ModeChanged(PlayModeStateChange s) { if (s is PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode) { if (!cam) cam = Stray(); Ensure(); ApplyPose(); } }

        static Camera Stray()
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<Camera>())
                if (c && c.name == CamName && (c.gameObject.hideFlags & HideFlags.DontSave) != 0) return c;
            return null;
        }

        void Release()
        {
            RestoreShadows();
            if (cam) DestroyImmediate(cam.gameObject);
            for (var c = Stray(); c; c = Stray()) DestroyImmediate(c.gameObject);
            cam = null;
        }

        void RestoreShadows()
        {
            if (shadowRestore > 0 && UniversalRenderPipeline.asset) UniversalRenderPipeline.asset.shadowDistance = shadowRestore;
            shadowRestore = -1f;
        }

        void Tick()
        {
            double now = EditorApplication.timeSinceStartup; float dt = (float)(now - last); last = now;
            if (!live || path == null || !playing) return;
            t += Mathf.Min(dt, 0.1f) * speed;
            if (t >= path.Duration) { if (loop) t = 0f; else { t = path.Duration; playing = false; } }
            ApplyPose();
            Repaint();
        }

        void ApplyPose()
        {
            if (!live || path == null) return;
            Ensure();
            var pose = path.Evaluate(t);
            pose.Apply(cam);
            if (shadows && UniversalRenderPipeline.asset)
            {
                if (shadowRestore < 0) shadowRestore = UniversalRenderPipeline.asset.shadowDistance;
                UniversalRenderPipeline.asset.shadowDistance = pose.ShadowDistance;
            }
            if (sceneView && SceneView.lastActiveSceneView is { } sv)
            {
                sv.orthographic = pose.Ortho;
                sv.cameraSettings.fieldOfView = pose.Fov;
                sv.AlignViewToObject(cam.transform);
                sv.Repaint();
            }
            RepaintGameViews();
        }

        static Type gameView;
        static void RepaintGameViews()
        {
            gameView ??= Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameView == null) return;
            foreach (var w in Resources.FindObjectsOfTypeAll(gameView)) ((EditorWindow)w).Repaint();
        }

        void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space) { playing = !playing; e.Use(); }
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (path == null) { if (GUILayout.Button("Load")) Load(); return; }

            var pose = path.Evaluate(t);
            EditorGUILayout.LabelField($"{t:F1}s / {path.Duration:F1}s   stop {pose.Stop + 1}/{path.Keys.Count}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(pose.Label + (pose.Ortho ? $"   (orthographic, zoom {pose.OrthoSize:F0})" : $"   (fov {pose.Fov:F0})"));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(playing ? "Pause" : "Play", GUILayout.Height(26))) playing = !playing;
                if (GUILayout.Button("|<", GUILayout.Width(36), GUILayout.Height(26))) Jump(pose.Holding ? pose.Stop - 1 : pose.Stop);
                if (GUILayout.Button(">|", GUILayout.Width(36), GUILayout.Height(26))) Jump(pose.Stop + 1);
                if (GUILayout.Button("Stop", GUILayout.Width(50), GUILayout.Height(26))) { playing = false; t = 0f; ApplyPose(); RestoreShadows(); }
            }
            EditorGUI.BeginChangeCheck();
            t = EditorGUILayout.Slider("Time", t, 0f, path.Duration);
            if (EditorGUI.EndChangeCheck()) ApplyPose();
            speed = EditorGUILayout.Slider("Speed", speed, 0.1f, 4f);
            loop = EditorGUILayout.Toggle("Loop", loop);
            EditorGUI.BeginChangeCheck();
            sceneView = EditorGUILayout.Toggle(new GUIContent("Drive Scene view", "Move the Scene view camera along with the flyby."), sceneView);
            shadows = EditorGUILayout.Toggle(new GUIContent("Shadow distance follows", "Temporarily sets the URP asset's shadow distance per stop; restored on Stop/close."), shadows);
            if (EditorGUI.EndChangeCheck()) { if (!shadows) RestoreShadows(); ApplyPose(); }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stops", EditorStyles.boldLabel);
            for (int i = 0; i < path.Keys.Count; i++)
                if (GUILayout.Button($"{i + 1}. {path.Keys[i].Name}", EditorStyles.miniButton)) Jump(i);
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload path")) Load();
                if (GUILayout.Button("Render frames → out/shots/flyby")) { playing = false; Release(); Flyby.RenderMenu(); }
            }
            EditorGUILayout.HelpBox("Watch in the Game view (the flyby camera draws over the play camera). Scene view roam: hold right mouse + WASD/QE, Shift = fast. In Play mode, the free-fly camera: Tab toggles, right mouse looks, WASD/QE, Shift fast.", MessageType.None);
        }

        void Jump(int stop)
        {
            stop = Mathf.Clamp(stop, 0, path.Keys.Count - 1);
            t = path.Keys[stop].Start;
            ApplyPose();
        }
    }
}
