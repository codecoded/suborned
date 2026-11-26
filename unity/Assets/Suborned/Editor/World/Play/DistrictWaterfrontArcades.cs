using System;
using System.Linq;
using Suborned.Play;
using Suborned.Sim;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Suborned.World.Play
{
    sealed partial class DistrictStreetArchitecture
    {
        Material[] Enamels(bool market)
        {
            const string key = "Harbour destination plates";
            if (!palette.TryGetValue(key,out var sign))
            {
                var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Suborned/Materials/Play/HarbourSigns.png");
                if (!atlas) throw new BuildFailedException("Harbour destination plates are missing");
                sign = new Material(Colour("Enamel_Ivory",true)) { name = key };
                sign.SetTexture("_BaseMap",atlas); sign.SetColor("_BaseColor", Color.white);
                sign.EnableKeyword("_EMISSION"); sign.SetTexture("_EmissionMap",atlas); sign.SetColor("_EmissionColor",new Color(.18f,.16f,.11f));
                palette.Add(key,sign);
            }
            return new[] { Colour("Enamel_Ivory",true), Colour("Enamel_Teal",true), Colour("Chrome",true),
                Colour("Fabric_Ivory",true), Colour(market ? "Fabric_Coral" : "Enamel_Seafoam",true), sign, Colour("Glow_Amber",true) };
        }

        static void Plaque(Surface s, float y, float z, float w, int id)
        {
            float h = w/4;
            s.Solid(1,0,z,w+.16f,.16f,y-h/2-.07f,h+.14f,.075f);
            s.Sign(5,new Vector3(0,y,z+.083f),w,h,id);
            s.Sign(5,new Vector3(0,y,z-.083f),w,h,id,true);
        }

        static Mesh StationName(int id, bool near)
        {
            var s = new Surface(7);
            s.Solid(0,0,0,7.16f,.43f,3.13f,.14f,.19f);
            s.Solid(1,0,0,6.80f,.46f,3.27f,.14f,.20f);
            Plaque(s,3.63f,0,3.15f,id);
            if (near)
                foreach (int side in new[]{-1,1})
                    for (int k=0;k<3;k++)
                        s.Solid(2,side*(2.25f-.12f*k),0,1.14f-.18f*k,.18f,3.40f+.12f*k,.055f,.07f);
            return s.Finish("Tramways enamel marquee");
        }

        static Mesh QuayBay(bool shade, int landing, bool near)
        {
            var s = new Surface(7);
            // The feet bolt to the outside of the coping. Local +Z faces the pedestrian deck.
            foreach (float x in new[]{-4.4f,0f,4.4f})
            {
                s.Solid(1,x,0,.26f,.24f,-.22f,.40f,.08f,2);
                s.Beam(2,new Vector3(x,.15f,0),new Vector3(x,1.00f,0),.075f,.085f);
            }
            foreach (float y in new[]{.46f,.92f})
                s.Beam(2,new Vector3(-4.48f,y,0),new Vector3(4.48f,y,0),.07f,.07f);
            if (shade)
            {
                foreach (float x in new[]{-3.65f,3.65f})
                {
                    s.Solid(1,x,0,.26f,.26f,-.20f,3.08f,.11f,3);
                    s.Beam(0,new Vector3(x,2.42f,0),new Vector3(x,3.55f,2.46f),.15f,.18f);
                    s.Beam(2,new Vector3(x,3.12f,0),new Vector3(x,3.86f,.84f),.095f,.10f);
                }
                s.Solid(0,0,.28f,8.54f,.94f,3.78f,.21f,.41f);
                s.Canvas(3,4,0,3.86f,.56f,8.10f,2.70f,near);
                s.Solid(1,0,3.23f,8.17f,.12f,2.58f,.12f,.055f,2);
                Plaque(s,3.41f,.06f,3.80f,landing);
                if (near) s.Solid(6,0,.63f,6.81f,.10f,3.74f,.045f,.03f,1);
            }
            return s.Finish(shade ? "Landing shade arcade" : "Harbour rail");
        }

        static Mesh MarketAwning(bool near, bool harbour)
        {
            var s = new Surface(7);
            // The masts pass through the existing vendor's stall. The open side remains the serving side.
            foreach (float x in new[]{-.75f,.75f})
            {
                s.Beam(2,new Vector3(x,.1f,0),new Vector3(x,3.79f,0),.12f,.13f);
                s.Beam(0,new Vector3(x,2.66f,0),new Vector3(x,3.30f,1.43f),.11f,.11f);
            }
            s.Solid(1,0,0,5.4f,.38f,3.73f,.14f,.18f);
            s.Canvas(3,4,0,3.80f,.05f,5.60f,2.37f,near);
            s.Canvas(3,4,0,3.97f,-1.06f,5.60f,1.13f,near);
            if (harbour) Plaque(s,4.18f,0,2.54f,7);
            return s.Finish("Striped market hall canopy");
        }

        void StreetPiece(Transform root, string key, Vector3 at, float yaw, bool market, Func<bool,Mesh> build)
        {
            if (!meshes.TryGetValue(key,out var pair)) meshes[key]=pair=(build(true),build(false));
            Draw(root,key,at,yaw,pair,Enamels(market),true);
        }

        public void LandingsAndStops(Transform root, SiteFrame frame, DistrictGround ground)
        {
            var stops = new[]{"terraces","lamplight","rocket","orbit","circus"};
            foreach (var stop in ground.Platforms)
            {
                string[] parts=stop.Id.Split('/');
                int id=parts.Length==4 ? Array.IndexOf(stops,parts[2]) : -1;
                if (id<0) continue;
                var opposite=ground.Platforms.First(p=>p.Id!=stop.Id && p.Id.StartsWith("stop/"+parts[1]+"/"+parts[2]+"/",StringComparison.Ordinal));
                var outward=frame.Dir(stop.Centre-opposite.Centre);
                var at=frame.World(stop.Centre)+outward*.78f; at.y=stop.WestCm*.01f;
                StreetPiece(root,"Tramways • "+stops[id],at,Mathf.Atan2(outward.x,outward.z)*Mathf.Rad2Deg,false,near=>StationName(id,near));
            }
            foreach (var strip in ground.Waterfront)
            {
                if (strip.Id=="quay/0") continue;
                bool pier=strip.B.Y-strip.A.Y>strip.A.X-strip.B.X;
                // The narrow finger at Market Pier and the seaward face of the Skyport landing.
                if (strip.Id=="quay/2" && !pier) continue;
                var a = frame.World(new Pt(strip.B.X, pier ? strip.A.Y : strip.B.Y));
                var b = frame.World(new Pt(pier ? strip.B.X : strip.A.X, strip.B.Y));
                var inward = frame.Dir(pier ? new Pt(100,0) : new Pt(0,-100));
                LandingPaving(root,a,b,inward,strip.EastCm*.01f,pier);
                int count = Mathf.FloorToInt(Vector3.Distance(a,b)/11f);
                for (int i=0;i<count;i++)
                {
                    var at=Vector3.Lerp(a,b,(i+.5f)/count)-inward*.17f; at.y=strip.EastCm*.01f;
                    bool shade=i%3==1; int landing=pier?5:6;
                    StreetPiece(root,(pier?"Market Pier":"Skyport Landing")+(shade?" arcade":" handrail"),at,
                        Mathf.Atan2(inward.x,inward.z)*Mathf.Rad2Deg,false,near=>QuayBay(shade,landing,near));
                }
            }
            foreach (var venue in ground.Venues)
            {
                if (venue.Id!="lantern-market" && venue.Id!="market" && venue.Id!="quay-terrace") continue;
                var stall=venue.Fixtures.FirstOrDefault(f=>f.Kit=="CoffeeCart_Play") ?? venue.Fixtures.FirstOrDefault(f=>f.Kit=="CurvedKiosk_Play");
                if (stall==null) continue;
                var at=frame.World(stall.At); at.y=ground.ElevationCm(stall.At)*.01f;
                bool harbour=venue.Id != "lantern-market";
                StreetPiece(root,harbour ? "Harbour Market canvas" : "Lantern Market canvas",at,stall.Yaw,true,near=>MarketAwning(near,harbour));
            }
        }
    }
}
