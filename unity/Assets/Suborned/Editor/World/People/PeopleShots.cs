using System.IO;
using Suborned.World.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Suborned.World.People
{
    /// <summary>Brief 010 E evidence: the people layer at 08:00, 09:00 (the guards walking to the café before their shift), 13:00 and 17:00 from the play camera (zoom 14, pitch 38, yaw 30,
    /// 1920x1080) over the café square (the export's play focus) and over the Records Office front (the same camera moved to the
    /// Office door), to &lt;repo&gt;/briefs/010/shots/people-&lt;HHMM&gt;-&lt;frame&gt;.png. Leaves the layer at 17:00 and saves the scene.
    /// Menu: Suborned/World/People/Shots (010). Batchmode (needs a GPU): -executeMethod Suborned.World.People.PeopleShots.Render</summary>
    public static class PeopleShots
    {
        static readonly int[] Clocks = { 8 * 60, 9 * 60, 13 * 60, 17 * 60 };
        /// <summary>Plan point the second frame centres on: the Records Office public entrance (-160, -281.75) and the square in front.</summary>
        static readonly Vector2 Office = new(-160f, -284f);

        [MenuItem("Suborned/World/People/Shots (010)")]
        public static void Render() => HarbourWorldBuilder.Batch(() =>
        {
            if (!Application.isBatchMode && !RenderSession.EnsureScene()) return;
            if (Application.isBatchMode) EditorSceneManager.OpenScene(HarbourWorldBuilder.ScenePath, OpenSceneMode.Single);
            var dir = Path.Combine(HarbourWorldBuilder.RepoRoot, "briefs", "010", "shots");
            Directory.CreateDirectory(dir);
            var play = GameObject.Find("cameras/play")?.GetComponent<Camera>() ?? throw new FileNotFoundException("People: no cameras/play in the scene");
            var playTs = play.GetComponent<TacticalShadows>();
            var focus = play.transform.position + play.transform.forward * playTs.focusDepth;
            var office = Object.Instantiate(play.gameObject); office.name = "people-office"; office.hideFlags = HideFlags.HideAndDontSave;
            var oc = office.GetComponent<Camera>(); oc.enabled = false;
            var of = new Vector3(Office.x, focus.y, Office.y);
            float stand = Rig.StandOff(play.transform.rotation, of, play.orthographicSize, 1920f / 1080f);
            office.transform.position = of - play.transform.forward * stand;
            office.GetComponent<TacticalShadows>().focusDepth = stand;
            if (office.TryGetComponent<ArchitectureFade>(out var af)) af.focus = of;
            try
            {
                using var session = new RenderSession();
                bool warm = false;
                foreach (var clock in Clocks)
                {
                    PeopleLayer.Snapshot(clock);
                    foreach (var (cam, frame) in new[] { (play, "cafe"), (oc, "office") })
                    {
                        if (!warm) { session.Grab(cam, 64, 64); warm = true; } // the first render after opening a scene comes out flat
                        using (cam.GetComponent<TacticalShadows>().Use())
                        {
                            var file = Path.Combine(dir, $"people-{PeopleLayer.Clock(clock).Replace(":", "")}-{frame}.png");
                            File.WriteAllBytes(file, session.Grab(cam, 1920, 1080).EncodeToPNG());
                            Debug.Log($"People: shot {file} (zoom {cam.orthographicSize}, pitch {cam.transform.eulerAngles.x:F0}, yaw {cam.transform.eulerAngles.y:F0})");
                        }
                    }
                }
            }
            finally { Object.DestroyImmediate(office); }
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new IOException("People: could not save the scene");
        });
    }
}
