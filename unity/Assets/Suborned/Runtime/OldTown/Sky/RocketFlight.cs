using UnityEngine;

namespace Suborned.OldTown.Sky
{
    /// <summary>
    /// A rocket liner's flight on a loop: an ascent (accelerating climb that pitches over toward a heading, leaving a
    /// billowing contrail that widens and fades with age) or a descent (braking onto a pad on its tail flame, a short
    /// hot plume). The contrail is a soft tube mesh rebuilt each frame (DK_Contrail.shader fades it at grazing
    /// angles, so it reads as vapour from any view). Frozen at editTime outside play mode for proof captures.
    /// </summary>
    [ExecuteAlways]
    public sealed class RocketFlight : MonoBehaviour
    {
        [SerializeField] GameObject rocketPrefab;
        [SerializeField] Material contrailMaterial;
        [SerializeField] bool descend;
        [SerializeField] float period = 150f;       // seconds per flight, then it repeats
        [SerializeField] float altitude = 6000f;    // ascent: height reached at the period's end; descent: start height
        [SerializeField] float heading = 45f;       // azimuth of the pitch-over (deg from +Z toward +X)
        [SerializeField] float downrange = .45f;    // horizontal travel as a fraction of altitude
        [SerializeField] float trailLife = 70f;     // seconds a contrail segment lasts
        [SerializeField] Vector2 trailWidth = new(4f, 90f);  // radius at birth, at trailLife
        [SerializeField] float editTime = 45f;

        const int Rings = 64, Sides = 10;
        Transform rocket;
        Mesh mesh;
        Vector3[] verts;
        Color[] colors;

        public void Configure(GameObject model, Material trail, bool landing, float seconds, float height, float azimuth,
                              float range, float life, Vector2 width, float frozenAt)
        {
            rocketPrefab = model; contrailMaterial = trail; descend = landing; period = seconds; altitude = height;
            heading = azimuth; downrange = range; trailLife = life; trailWidth = width; editTime = frozenAt;
            Build();
            Fly(editTime);
        }

        void OnEnable() { Build(); Fly(Clock); }

        void OnDisable()
        {
            if (rocket) DestroyImmediate(rocket.gameObject);
            if (mesh) DestroyImmediate(mesh);
            var trail = transform.Find("Contrail");
            if (trail) DestroyImmediate(trail.gameObject);
        }

        void Update() => Fly(Clock);

        float Clock => Application.isPlaying ? Time.time + editTime : editTime;

        /// Pose the flight `seconds` after its frozen moment (offline capture: a fly-through steps it frame by frame).
        public void PoseAt(float seconds) => Fly(editTime + seconds);

        /// Launch-relative position at flight time t (0..period).
        Vector3 At(float t)
        {
            float u = Mathf.Clamp01(t / period);
            Vector3 dir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            if (descend)
            {
                float d = 1f - u;  // braking: height falls with the square of the time left
                return Vector3.up * (altitude * d * d) + dir * (altitude * downrange * d * d * d);
            }
            return Vector3.up * (altitude * u * u) + dir * (altitude * downrange * u * u * u);
        }

        void Build()
        {
            OnDisable();
            if (rocketPrefab == null || contrailMaterial == null) return;
            rocket = Instantiate(rocketPrefab, transform).transform;
            rocket.gameObject.hideFlags = HideFlags.DontSave;
            var trail = new GameObject("Contrail") { hideFlags = HideFlags.DontSave };
            trail.transform.SetParent(transform, false);
            trail.AddComponent<MeshFilter>().sharedMesh = mesh = new Mesh { name = "Contrail", hideFlags = HideFlags.DontSave };
            var mr = trail.AddComponent<MeshRenderer>();
            mr.sharedMaterial = contrailMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            verts = new Vector3[Rings * Sides];
            colors = new Color[verts.Length];
            var normals = new Vector3[verts.Length];
            var uv = new Vector2[verts.Length];
            var tris = new int[(Rings - 1) * Sides * 6];
            for (int r = 0, k = 0; r < Rings - 1; r++)
                for (int s = 0; s < Sides; s++)
                {
                    int a = r * Sides + s, b = r * Sides + (s + 1) % Sides;
                    tris[k++] = a; tris[k++] = b; tris[k++] = a + Sides;
                    tris[k++] = b; tris[k++] = b + Sides; tris[k++] = a + Sides;
                }
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(i / Sides / (Rings - 1f), i % Sides / (float)Sides);
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = tris;
            mesh.MarkDynamic();
        }

        void Fly(float clock)
        {
            if (rocket == null || mesh == null) return;
            float t = Mathf.Repeat(clock, period);
            Vector3 p = At(t), v = At(t + .5f) - At(t - .5f);
            Vector3 up = descend ? Vector3.up : (v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.up);
            rocket.localPosition = p;
            rocket.localRotation = Quaternion.FromToRotation(Vector3.up, up);

            // contrail: rings from the nozzle back through the path's history (descents: a short hot plume)
            float life = descend ? 3f : Mathf.Min(trailLife, t);
            var normals = mesh.normals;
            for (int r = 0; r < Rings; r++)
            {
                float f = r / (Rings - 1f), age = life * f;
                Vector3 c = At(Mathf.Max(0f, t - age)) - (descend ? Vector3.up * (4f + 40f * f) : Vector3.zero);
                Vector3 axis = (At(t - age + .5f) - At(t - age - .5f)).normalized;
                if (axis.sqrMagnitude < .5f) axis = Vector3.up;
                Vector3 side = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized;
                Vector3 side2 = Vector3.Cross(axis, side);
                float lifeF = descend ? f : age / trailLife;
                float radius = descend ? Mathf.Lerp(1.2f, 6f, f) : Mathf.Lerp(trailWidth.x, trailWidth.y, Mathf.Pow(lifeF, .6f));
                float alpha = descend ? 1f - f : Mathf.Pow(1f - lifeF, 1.4f) * Mathf.SmoothStep(0f, 1f, f * 12f);
                for (int s = 0; s < Sides; s++)
                {
                    float a = s * Mathf.PI * 2f / Sides;
                    Vector3 n = side * Mathf.Cos(a) + side2 * Mathf.Sin(a);
                    int i = r * Sides + s;
                    verts[i] = c + n * radius;
                    normals[i] = n;
                    colors[i] = new Color(descend ? 1f : 0f, lifeF, 0f, alpha);  // r: hot plume, g: age
                }
            }
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }
    }
}
