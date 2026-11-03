using UnityEngine;

namespace Suborned.OldTown.Sky
{
    /// <summary>
    /// A Catmull-Rom route through world-space points (a sky-rail loop, an aircar lane), arc-length parameterised
    /// so movers advance at a true speed. Closed paths wrap; open paths clamp.
    /// </summary>
    public sealed class SkyPath : MonoBehaviour
    {
        [SerializeField] Vector3[] points = System.Array.Empty<Vector3>();
        [SerializeField] bool closed = true;
        [SerializeField, Min(2)] int samplesPerSpan = 24;

        Vector3[] samples;
        float[] distances;

        public bool Closed => closed;
        public float Length { get { Ensure(); return distances[^1]; } }

        public void Set(Vector3[] route, bool loop)
        {
            points = route;
            closed = loop;
            samples = null;
        }

        void OnValidate() => samples = null;

        /// Position at distance s along the path (wraps when closed).
        public Vector3 Point(float s)
        {
            Ensure();
            float len = distances[^1];
            s = closed ? Mathf.Repeat(s, len) : Mathf.Clamp(s, 0f, len);
            int lo = 0, hi = distances.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (distances[mid] <= s) lo = mid; else hi = mid;
            }
            float span = distances[hi] - distances[lo];
            return Vector3.Lerp(samples[lo], samples[hi], span > 1e-5f ? (s - distances[lo]) / span : 0f);
        }

        public Vector3 Tangent(float s, float d = 2f) => (Point(s + d) - Point(s - d)).normalized;

        /// Distance along the path of the sample nearest to p.
        public float Nearest(Vector3 p)
        {
            Ensure();
            int best = 0;
            for (int i = 1; i < samples.Length; i++)
                if ((samples[i] - p).sqrMagnitude < (samples[best] - p).sqrMagnitude) best = i;
            return distances[best];
        }

        /// Signed horizontal curvature (1/m, + = turning right) for banking.
        public float Curvature(float s, float d = 12f)
        {
            Vector3 a = Tangent(s - d), b = Tangent(s + d);
            a.y = b.y = 0f;
            return Vector3.SignedAngle(a, b, Vector3.up) * Mathf.Deg2Rad / (2f * d);
        }

        void Ensure()
        {
            if (samples != null) return;
            int n = points.Length, spans = closed ? n : n - 1;
            samples = new Vector3[spans * samplesPerSpan + 1];
            distances = new float[samples.Length];
            for (int i = 0; i < spans; i++)
                for (int k = 0; k < samplesPerSpan; k++)
                    samples[i * samplesPerSpan + k] = CatmullRom(i, k / (float)samplesPerSpan);
            samples[^1] = closed ? samples[0] : points[n - 1];
            for (int i = 1; i < samples.Length; i++)
                distances[i] = distances[i - 1] + Vector3.Distance(samples[i - 1], samples[i]);
        }

        Vector3 P(int i) => closed ? points[(i % points.Length + points.Length) % points.Length]
                                   : points[Mathf.Clamp(i, 0, points.Length - 1)];

        Vector3 CatmullRom(int i, float t)  // centripetal-ish uniform form; routes are gently curved
        {
            Vector3 p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
            float t2 = t * t, t3 = t2 * t;
            return .5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
