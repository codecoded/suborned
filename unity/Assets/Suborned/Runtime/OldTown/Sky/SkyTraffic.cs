using UnityEngine;

namespace Suborned.OldTown.Sky
{
    /// <summary>
    /// Vehicles circulating on a SkyPath: evenly spaced units (a train = several coupled cars), moving at a set speed,
    /// banked into curves like aircraft (roll = atan(v^2 k / g)) with an optional gentle bob. Runs in edit mode too,
    /// frozen at editTime, so proof captures show the traffic placed along its lanes.
    /// </summary>
    [ExecuteAlways]
    public sealed class SkyTraffic : MonoBehaviour
    {
        [SerializeField] SkyPath path;
        [Tooltip("Cycled per unit; with cars > 1: [head, middle] (the last car is the head turned about)")]
        [SerializeField] GameObject[] prefabs = System.Array.Empty<GameObject>();
        [SerializeField, Min(1)] int units = 6;
        [SerializeField, Min(1)] int cars = 1;
        [SerializeField] float carPitch = 18.4f;
        [SerializeField] float speed = 30f;
        [SerializeField, Range(0f, 1f)] float bank = 1f;
        [SerializeField] float bob;
        [SerializeField] float editTime;

        Transform[] spawned;

        public void Configure(SkyPath route, GameObject[] models, int count, int carCount, float pitch, float metresPerSecond,
                              float banking, float bobbing, float frozenAt)
        {
            path = route; prefabs = models; units = count; cars = carCount; carPitch = pitch; speed = metresPerSecond;
            bank = banking; bob = bobbing; editTime = frozenAt;
            Spawn();
            Place(editTime);
        }

        void OnEnable() { Spawn(); Place(Clock); }

        void OnDisable()
        {
            if (spawned == null) return;
            foreach (var t in spawned) if (t) DestroyImmediate(t.gameObject);
            spawned = null;
        }

        void Update() => Place(Clock);

        float Clock => Application.isPlaying ? Time.time + editTime : editTime;

        /// Pose the traffic `seconds` after its frozen moment (offline capture: a fly-through steps it frame by frame).
        public void PoseAt(float seconds) => Place(editTime + seconds);

        void Spawn()
        {
            OnDisable();
            if (path == null || prefabs.Length == 0) return;
            spawned = new Transform[units * cars];
            for (int u = 0; u < units; u++)
                for (int c = 0; c < cars; c++)
                {
                    var model = cars > 1 ? prefabs[c == 0 || c == cars - 1 ? 0 : Mathf.Min(1, prefabs.Length - 1)]
                                         : prefabs[u % prefabs.Length];
                    if (model == null) continue;
                    var go = Instantiate(model, transform);
                    go.hideFlags = HideFlags.DontSave;  // rebuilt on enable; never serialised into the scene
                    go.name = $"{model.name} {u}.{c}";
                    spawned[u * cars + c] = go.transform;
                }
        }

        void Place(float time)
        {
            if (spawned == null || path == null) return;
            float len = path.Length, gap = len / units;
            for (int u = 0; u < units; u++)
                for (int c = 0; c < cars; c++)
                {
                    var t = spawned[u * cars + c];
                    if (t == null) continue;
                    float s = speed * time + u * gap - c * carPitch;
                    Vector3 fwd = path.Tangent(s);
                    float roll = -Mathf.Atan(speed * speed * path.Curvature(s) / 9.81f) * Mathf.Rad2Deg * bank;
                    var rot = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, 0f, roll);
                    if (cars > 1 && c == cars - 1) rot *= Quaternion.Euler(0f, 180f, 0f);  // tail car: nose aft
                    float lift = bob * Mathf.Sin(time * .7f + u * 1.9f);
                    t.SetPositionAndRotation(path.Point(s) + Vector3.up * lift, rot);
                }
        }
    }
}
