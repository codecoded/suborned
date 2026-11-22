using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    public sealed partial class HostViews
    {
        // The seal clock drives the approach, including pause, fast-forward and replay.
        // Its final pose occupies the outbound lane on the exact tick travel ends.
        const float CloseRoadSeconds = 8f;
        Transform cordon;
        float sealedSince = -1f;
        readonly Transform[] cruisers = new Transform[2], officers = new Transform[4], walkers = new Transform[2], eyes = new Transform[2];
        readonly Animator[] officerPose = new Animator[4];
        readonly Transform[] lamps = new Transform[4];
        public Vector3? SealedRoad => cordon && cordon.gameObject.activeSelf ? cordon.position : (Vector3?)null;

        internal static bool EndedAtCordon(Game g) => g.FailCode == "sealed"
            || g.FailCode == "closed-in" && g.CloseWarnedAt >= 0
            || g.Over && g.Tick >= g.SplitPoliceAt && g.Captured.Count > 0;

        void RoadClosure()
        {
            var g = host.Game;
            int closes = Mathf.Min(g.Lockdown, g.SplitPoliceAt);
            if (g.CloseWarnedAt >= 0) closes = Mathf.Min(closes, g.CloseWarnedAt + g.R.CloseWarnS * Game.Hz);
            bool sealedNow = g.Over && EndedAtCordon(g);
            float left = sealedNow ? 0f : (closes - g.Tick) / (float)Game.Hz;
            bool visible = !g.Range && g.CurStand != null && left <= CloseRoadSeconds && (!g.Over || sealedNow);
            if (!visible) { if (cordon && cordon.gameObject.activeSelf) cordon.gameObject.SetActive(false); return; }
            if (!cordon) BuildCordon();
            cordon.gameObject.SetActive(true);
            var st = g.CurStand;
            var end = frame.World(g.S.LanePointAt(1500, st));
            var dir = end - frame.World(g.S.LanePointAt(900, st));
            if (dir.sqrMagnitude < 0.01f) dir = frame.Dir(st.Facing);
            dir.y = 0f;
            cordon.SetPositionAndRotation(end, Quaternion.LookRotation(dir, Vector3.up));
            if (g.Over && sealedSince < 0f) sealedSince = Time.unscaledTime;
            float settle = g.Over ? Mathf.Min(2f, Time.unscaledTime - sealedSince) : 0f;
            float motionTick = g.Tick + (g.Over ? (Time.unscaledTime - sealedSince) * Game.Hz : 0f);
            float t = Mathf.Clamp01(1f - left / CloseRoadSeconds);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float stop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f * i, 0.70f + 0.30f * i, t));
                var parked = new Vector3(side * 2.3f, 0f, i * 1.8f);
                cruisers[i].localPosition = parked + Vector3.forward * (18f * (1f - stop));
                cruisers[i].localRotation = Quaternion.Euler(0f, 180f + side * 68f * stop, 0f);
                float walk = Mathf.SmoothStep(0f, 1f, t);
                walkers[i].localPosition = new Vector3(side * 4.4f, 0f, Mathf.Lerp(12f + 2f * i, -1f, walk));
                eyes[i].localPosition = new Vector3(side * Mathf.Lerp(8f, 3.2f, walk), 4.4f + i + 0.16f * Mathf.Sin(motionTick * 0.35f + i), Mathf.Lerp(22f, -2.5f, walk));
                for (int k = 0; k < 2; k++)
                {
                    int n = i * 2 + k;
                    float step = Mathf.Clamp01((t + settle / CloseRoadSeconds - 0.64f - 0.22f * i - 0.05f * k) / 0.3f);
                    officers[n].gameObject.SetActive(step > 0f);
                    officers[n].localPosition = parked + new Vector3(side * (k - 0.5f) * 1.6f, 0f, Mathf.Lerp(-0.8f, -2.9f - k, step));
                    if (step > 0f && officerPose[n]) officerPose[n].Play(step < 1f ? "walk" : "idle", 0, step < 1f ? step * 2.4f : 0f);
                    lamps[n].gameObject.SetActive(((Mathf.FloorToInt(motionTick / 3f) + n) & 1) == 0);
                }
            }
        }

        void BuildCordon()
        {
            cordon = new GameObject("District road closure").transform;
            cordon.SetParent(root, false);
            Transform Place(GameObject asset, string label)
            {
                var t = asset ? Object.Instantiate(asset, cordon, false).transform : new GameObject(label).transform;
                t.SetParent(cordon, false); t.name = label; t.localRotation = Quaternion.Euler(0f, 180f, 0f);
                foreach (var collider in t.GetComponentsInChildren<Collider>()) collider.enabled = false;
                return t;
            }
            var red = Unlit(new Color(1f, 0.16f, 0.055f)); var blue = Unlit(new Color(0.1f, 0.65f, 1f));
            var security = host.figures?.Find("security");
            for (int i = 0; i < 2; i++)
            {
                cruisers[i] = Place(host.sedanOfficial, "Response sedan");
                var paint = new MaterialPropertyBlock();
                paint.SetColor("_BaseColor", new Color(0.035f, 0.07f, 0.13f));
                foreach (var r in cruisers[i].GetComponentsInChildren<Renderer>())
                {
                    var materials = r.sharedMaterials;
                    for (int slot = 0; slot < materials.Length; slot++)
                        if (materials[slot] && materials[slot].name.EndsWith("_Teal", System.StringComparison.Ordinal))
                            r.SetPropertyBlock(paint, slot);
                }
                walkers[i] = Place(host.robotUnit, "Cordon robot");
                eyes[i] = Place(host.hoverCamera, "Overhead patrol");
                eyes[i].localScale *= HoverScale;
                for (int k = 0; k < 2; k++)
                {
                    int n = 2 * i + k;
                    lamps[n] = Part(cruisers[i], "Beacon", sphere, k == 0 ? red : blue, new Vector3((k - 0.5f) * 0.7f, 1.55f, 0f), new Vector3(0.33f, 0.23f, 0.33f));
                    officers[n] = Place(security?.model, "Cordon officer");
                    if (security?.model != null)
                    {
                        var animator = officers[n].GetComponent<Animator>() ?? officers[n].gameObject.AddComponent<Animator>();
                        animator.runtimeAnimatorController = host.figures.ControllerFor(security);
                        animator.applyRootMotion = false; officerPose[n] = animator;
                    }
                }
            }
        }
    }
}
