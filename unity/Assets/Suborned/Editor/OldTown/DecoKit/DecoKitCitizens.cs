#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Suborned.OldTown.Editor.DecoKit
{
    /// The city's people and cars: the RetroFutureMobility glTFs (MPFB citizens ~25k tris, cars ~22k, their own
    /// materials; the cars' kit is ~1.6x scale, so each is scaled to a 1.95 m track). Seven people and five bodies
    /// would repeat in a crowd, so each citizen takes a coat and each car an enamel from the ensemble's palette (one
    /// shared material per source and colour), and people vary +-5% in height. Seeded, so rebuilds match.
    internal sealed class DecoKitCitizens
    {
        const string Rfm = "Assets/Suborned/OldTownArt/RetroFutureMobility/";
        const float CarWidth = 1.95f, CarClear = 5.5f;
        static readonly string[] PeopleAssets =
        {
            "People/RFM_PeopleCommuterV2", "People/RFM_PeopleCourierV2", "People/RFM_PeopleDinerOwnerV2", "People/RFM_PeopleElderV2",
            "People/RFM_PeopleMechanicV2", "People/RFM_PeopleTransitOfficerV2", "People/RFM_CityCommuterV1",
        };
        static readonly string[] CarAssets =
        {
            "Ground/RFM_CivilianBubbleMicroV1", "Ground/RFM_CivilianFamilyWagonV1", "Ground/RFM_CivilianStreamlineTaxiV1",
            "Ground/RFM_CivilianNeighborhoodShuttleV1", "Ground/RFM_CivilianBubbleMicroV1", "Ground/RFM_CivilianFamilyWagonV1",
            "Ground/RFM_ServiceDeliveryVanV1",
        };
        static readonly Regex Coat = new(@"(cloth|wool)$");        // the V2 citizens' coats: flat factors, no texture
        static readonly Regex Enamel = new(@"turquoise enamel$");  // the cars' accent panels (the bodies stay ivory)
        static readonly Color[] Coats = Hex("#1F2E4A", "#23483A", "#A87A4E", "#6E2632", "#C48E2F", "#1E5E66", "#C9644C",
                                            "#8C9396", "#34373A", "#D6CAB0", "#4B3150", "#0E4556");
        static readonly Color[] Enamels = Hex("#0A6D82", "#E25E47", "#E7B84C", "#78C4A2", "#B53A31", "#5F9FC6", "#27406A", "#2F7D5C");
        static readonly int BaseColorFactor = Shader.PropertyToID("baseColorFactor"), BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Dictionary<string, GameObject> assets = new();
        static readonly Dictionary<string, float> carScale = new();
        static readonly Dictionary<(Material, int), Material> tints = new();

        readonly Transform root;
        readonly System.Random rng;
        readonly List<Vector3> cars = new();
        internal int People { get; private set; }
        internal int Cars => cars.Count;
        internal static int Tints => tints.Count;

        internal DecoKitCitizens(string name, Transform parent, int seed)
        {
            root = new GameObject(name).transform;
            root.SetParent(parent, false);
            rng = new System.Random(seed);
        }

        float R(float a, float b) => a + (b - a) * (float)rng.NextDouble();

        internal void Person(Vector3 at, float yaw)
        {
            var go = Spawn(PeopleAssets[rng.Next(PeopleAssets.Length)], at, yaw, R(.95f, 1.05f));
            if (go == null) return;
            Tint(go, Coat, Coats);
            People++;
        }

        internal void Car(Vector3 at, float yaw)
        {
            var name = CarAssets[rng.Next(CarAssets.Length)];
            if (!carScale.TryGetValue(name, out var s))
            {
                var probe = Spawn(name, Vector3.down * 500f, 0f, 1f);
                carScale[name] = s = probe ? CarWidth / Mathf.Max(DecoKitModels.Bounds(probe).size.x, .1f) : 1f;
                if (probe) Object.DestroyImmediate(probe);
            }
            var go = Spawn(name, at, yaw, s);
            if (go == null) return;
            Tint(go, Enamel, Enamels);
            cars.Add(at);
        }

        /// A car's length clear of every car placed.
        internal bool NearCar(Vector3 at) => cars.Any(q => (q - at).sqrMagnitude < CarClear * CarClear);

        /// Moving traffic down one lane, a to b: a car every gap.x-gap.y m where `blocked` allows and no car stands.
        internal void Lane(Vector2 a, Vector2 b, Vector2 gap, System.Func<Vector3, bool> blocked)
        {
            var d = (b - a).normalized;
            float len = Vector2.Distance(a, b), yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            for (float t = R(2f, gap.y * .5f); t < len - 3f; t += R(gap.x, gap.y))
            {
                var c = a + d * t;
                var at = new Vector3(c.x, 0f, c.y);
                if (!blocked(at) && !NearCar(at)) Car(at, yaw);
            }
        }

        /// Swaps the matching materials for one palette colour, picked per instance.
        void Tint(GameObject go, Regex match, Color[] palette)
        {
            int k = rng.Next(palette.Length);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !match.IsMatch(mats[i].name)) continue;
                    if (!tints.TryGetValue((mats[i], k), out var t) || t == null)
                    {
                        t = new Material(mats[i]) { name = $"{mats[i].name} #{k}" };
                        if (t.HasProperty(BaseColorFactor)) t.SetColor(BaseColorFactor, palette[k]);
                        if (t.HasProperty(BaseColor)) t.SetColor(BaseColor, palette[k]);
                        tints[(mats[i], k)] = t;
                    }
                    mats[i] = t;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// A glTF at `at` (its bounds' base on at.y), turned and scaled; null if the asset is missing.
        GameObject Spawn(string name, Vector3 at, float yaw, float scale)
        {
            if (!assets.TryGetValue(name, out var asset)) assets[name] = asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Rfm}{name}.glb");
            if (asset == null) { Debug.LogWarning($"[DecoKit] citizens: no {Rfm}{name}.glb"); return null; }
            var go = PrefabUtility.InstantiatePrefab(asset, root) as GameObject ?? Object.Instantiate(asset, root);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            var bounds = DecoKitModels.Bounds(go);
            go.transform.position += Vector3.up * (at.y - bounds.min.y);
            return go;
        }

        static Color[] Hex(params string[] hex) =>
            hex.Select(h => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta).ToArray();
    }
}
#endif
