using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Suborned.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Suborned.Play.Tests
{
    /// <summary>Brief 077 (Latch): the testbed planting rows through KitLods. A row's own LOD levels (three beyond L0) at its own switch heights; its
    /// `lodSize` as the LODGroup's size (set after SetLODs, which resets it); its soil objects (`objects[].shadows` off) casting no shadow at every level;
    /// and the play-zoom guard exempting a group whose row declares `lodAtPlay` (Sky 2026-10-30: gameplay LODs at play zoom), never by id. The LOD
    /// fixtures are prefabs written to a git-ignored probe folder and removed after.</summary>
    public sealed class KitLods077Tests
    {
        const string Dir = "Assets/Scenes/probe-tests077";
        static readonly float[] Screens = { .6667f, .2963f, .1667f };
        Material mat;
        readonly List<Object> made = new();

        [SetUp]
        public void SetUp()
        {
            mat = KitImport.Library("DK_Limestone");
            if (!mat) Assert.Ignore("the kit library (Kit/Materials/DK_Limestone) is not copied here: run `bun run kit`");
            Directory.CreateDirectory(Path.Combine(HarbourWorldBuilder.RepoRoot, "unity", Dir));
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in made) if (o) Object.DestroyImmediate(o);
            made.Clear();
            AssetDatabase.DeleteAsset(Dir);
        }

        GameObject Piece(string name, string suffix)
        {
            var root = new GameObject(name + suffix); made.Add(root);
            foreach (var part in new[] { name + suffix, name + suffix + "_soil" })
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube); c.name = part; Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(root.transform, false); c.transform.localScale = new Vector3(10f, .5f, 2f);
                c.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            return root;
        }

        (GameObject go, List<(string, float)> lods) Fixture()
        {
            var lods = new List<(string, float)>();
            for (int i = 1; i <= 3; i++)
            {
                var g = Piece("DK_T", "_L" + i); var path = $"{Dir}/DK_T_L{i}.prefab";
                PrefabUtility.SaveAsPrefabAsset(g, path); lods.Add((path, Screens[i - 1]));
            }
            return (Piece("DK_T", ""), lods);
        }

        static readonly KitLods.Row Row = new() { LodSize = 4f, Objects = new[] { ("DK_T", true), ("DK_T_soil", false) } };

        [Test]
        public void TheRowsThreeLevelsAtItsOwnHeights()
        {
            var (go, lods) = Fixture();
            KitLods.Piece("T", "Plaza", go, lods, null, Row);
            var lg = go.GetComponent<LODGroup>(); Assert.That(lg, Is.Not.Null, "a LODGroup");
            var h = lg.GetLODs().Select(l => l.screenRelativeTransitionHeight).ToArray();
            Assert.That(h.Length, Is.EqualTo(4), "L0..L3, the last culling");
            for (int i = 0; i < 3; i++) Assert.That(h[i], Is.EqualTo(Screens[i]).Within(1e-5f), $"L{i} hands over at the row's lods[{i}].screen");
        }

        [Test]
        public void LodSizeIsTheGroupsSize()
        {
            var (go, lods) = Fixture();
            KitLods.Piece("T", "Plaza", go, lods, null, Row);
            var lg = go.GetComponent<LODGroup>();
            Assert.That(lg.size, Is.EqualTo(4f).Within(1e-5f), "LODGroup.size = the row's lodSize (the 10 m fixture would give 10)");
            Assert.That(lg.GetLODs().Last().screenRelativeTransitionHeight, Is.EqualTo(.01f).Within(1e-5f), "the cull from the 4 m size: a small piece's 1 %");
        }

        [Test]
        public void SoilCastsNoShadowAtAnyLevel()
        {
            var (go, lods) = Fixture();
            KitLods.Piece("T", "Plaza", go, lods, null, Row);
            var levels = go.GetComponent<LODGroup>().GetLODs();
            foreach (var (l, i) in levels.Select((l, i) => (l, i)))
                foreach (var r in l.renderers)
                {
                    bool soil = KitLods.Part(r.name).EndsWith("_soil", StringComparison.Ordinal) || r.transform.parent && KitLods.Part(r.transform.parent.name).EndsWith("_soil", StringComparison.Ordinal);
                    Assert.That(r.shadowCastingMode, Is.EqualTo(soil ? ShadowCastingMode.Off : ShadowCastingMode.On), $"level {i} {r.name}");
                }
        }

        [Test]
        public void ASingleObjectPieceIsItsOnlyRenderer()
        {
            // Unity names a one-object FBX's renderer after the file: the row's internal object name never matches, and must not reject
            var go = new GameObject("DK_U_TB"); made.Add(go); go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx"); go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            Assert.DoesNotThrow(() => KitLods.ObjectShadows("U_TB", go, new[] { ("DK_U", true) }), "a shadows-on object that is the piece itself");
            Assert.That(KitLods.ObjectShadows("U_TB", go, new[] { ("DK_U", false) }), Is.EqualTo(1), "a one-object row binds the one renderer");
            Assert.That(go.GetComponent<MeshRenderer>().shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            var two = Piece("DK_V", ""); 
            Assert.Throws<InvalidDataException>(() => KitLods.ObjectShadows("V", two, new[] { ("DK_V", true), ("DK_W_soil", false) }), "a shadows-off object with no part still rejects");
        }

        [Test]
        public void TheGuardExemptsDeclaredRowsOnly()
        {
            var (go, lods) = Fixture();
            KitLods.Piece("T", "Plaza", go, lods, null, Row);
            var root = new GameObject("world"); made.Add(root);
            var civic = new GameObject("civic"); civic.transform.SetParent(root.transform, false);
            go.transform.SetParent(civic.transform, false);
            Assert.DoesNotThrow(() => KitLods.PlayZoomGuard(root.transform, g => g == go.GetComponent<LODGroup>()), "a declared lodAtPlay row passes");
            Assert.Throws<InvalidDataException>(() => KitLods.PlayZoomGuard(root.transform, _ => false), "an undeclared one still fails the civic layer");
        }

        [Test]
        public void TheCatalogueFieldsAreRead()
        {
            var file = Path.Combine(HarbourWorldBuilder.RepoRoot, "unity", Dir, "kit.json");
            File.WriteAllText(file, @"{""assets"": [
 {""id"": ""P_TB"", ""status"": ""adopted"", ""source"": ""generated"", ""category"": ""Plaza"", ""files"": [{""role"": ""fbx"", ""path"": ""Models/Plaza/DK_P_TB.fbx""}],
  ""lodSize"": 4, ""lodAtPlay"": true, ""objects"": [{""name"": ""DK_P"", ""shadows"": ""on""}, {""name"": ""DK_P_soil"", ""shadows"": ""off""}]},
 {""id"": ""Q"", ""status"": ""adopted"", ""source"": ""generated"", ""category"": ""Plaza"", ""files"": [{""role"": ""fbx"", ""path"": ""Models/Plaza/DK_Q.fbx""}]}]}");
            var kit = new KitPlacer(file);
            Assert.That(kit.LodSizeOf("P_TB"), Is.EqualTo(4f));
            Assert.That(kit.AtPlay("P_TB"), Is.True, "lodAtPlay declared");
            Assert.That(kit.ObjectsOf("P_TB"), Is.EqualTo(new[] { ("DK_P", true), ("DK_P_soil", false) }));
            Assert.That(kit.LodSizeOf("Q"), Is.EqualTo(0f)); Assert.That(kit.AtPlay("Q"), Is.False, "not declared: never exempt"); Assert.That(kit.ObjectsOf("Q"), Is.Empty);
        }
    }
}
