using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Suborned.World.Play
{
    // Bake the market's working interiors and static shop rows into the scene on their original footprints.
    static class DistrictExchangeKit
    {
        const string Textures = "Assets/Suborned/Materials/Play/LanternKit/";

        static Material Finish()
        {
            Texture2D Map(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + file)
                ?? throw new BuildFailedException("Missing Lantern Exchange material map " + file);
            var shader = Shader.Find("DecoKit/Lit Cutaway");
            if (!shader) throw new BuildFailedException("Missing cutaway shader for Lantern Exchange");
            var mat = new Material(shader) { name = "Lantern Exchange • faience and shop glass", enableInstancing = true };
            mat.SetTexture("_BaseMap", Map("LanternBase.jpg"));
            var mask = Map("LanternMask.png");
            mat.SetTexture("_MetallicGlossMap", mask);
            mat.SetTexture("_OcclusionMap", mask);
            mat.SetFloat("_Smoothness", .90f);
            mat.SetFloat("_OcclusionStrength", .8f);
            mat.SetTexture("_BumpMap", Map("LanternNormal.png"));
            mat.SetFloat("_BumpScale", .62f);
            mat.SetTexture("_EmissionMap", Map("LanternLight.png"));
            mat.SetColor("_EmissionColor", new Color(1.00f,.90f,.77f));
            foreach (var keyword in new[] { "_NORMALMAP", "_METALLICSPECGLOSSMAP", "_OCCLUSIONMAP", "_EMISSION" })
                mat.EnableKeyword(keyword);
            return mat;
        }

        public static void Install(Scene scene, Transform district)
        {
            var roots = scene.GetRootGameObjects();
            var court = roots.SelectMany(r => r.GetComponentsInChildren<LODGroup>())
                .FirstOrDefault(g => g.GetLODs().Any(l => l.renderers.Any(r => r && r.name == "CourtMarket Close")));
            if (!court) return;
            var old = court.GetLODs().SelectMany(l => l.renderers).First(r => r && r.name == "CourtMarket Close");
            if (!old.enabled) return;
            var datum = old.transform;
            while (datum.parent && datum.name != "CourtMarket") datum = datum.parent;
            if (datum.name != "CourtMarket") throw new BuildFailedException("Lantern Court has no market-wing datum");
            var at = datum.position;
            var finish = Finish();
            var wings = new DistrictFerryGardens.Kit("LanternKit/ExchangeCourt", finish, .42f, .145f, .005f);
            wings.Place(district, at, Quaternion.identity);
            foreach (var r in court.GetLODs().SelectMany(l => l.renderers)) if (r) r.enabled = false;

            // A run mates three six-metre Old Town shells. Verify every socket before replacing its visual.
            // The landmark datum follows the planned court through the Old Town frame translation.
            var rows = new[] { (57f, -44.5f, 0f), (102f, -50f, 270f), (102f, 2f, 270f) };
            var bays = roots.SelectMany(r => r.GetComponentsInChildren<MeshFilter>())
                .Where(f => f.sharedMesh && f.sharedMesh.name == "DK_StreamBay_3")
                .Select(f => f.GetComponent<MeshRenderer>()).Where(r => r && r.enabled).ToArray();
            var shops = new DistrictFerryGardens.Kit("LanternKit/ExchangeShops", finish, .32f, .105f, .004f);
            int runs = 0;
            foreach (var (dx,dz,yaw) in rows)
            {
                var centre = at + new Vector3(dx,0,dz);
                var along = Quaternion.Euler(0,yaw,0) * Vector3.right;
                var sockets = new MeshRenderer[3];
                for (int i = 0; i < sockets.Length; i++)
                {
                    var seat = centre + (i-1)*6f*along;
                    sockets[i] = bays.FirstOrDefault(r => (r.transform.position-seat).sqrMagnitude < .04f &&
                        Mathf.Abs(Mathf.DeltaAngle(r.transform.eulerAngles.y,yaw)) < 1f);
                }
                if (Array.Exists(sockets, r => !r)) continue;
                shops.Place(district, centre, Quaternion.Euler(0,yaw+180f,0));
                foreach (var r in sockets) r.enabled = false;
                runs++;
            }
            Debug.Log($"Lantern Exchange: two court wings and {runs} three-bay runs using one shared material");
        }
    }
}
