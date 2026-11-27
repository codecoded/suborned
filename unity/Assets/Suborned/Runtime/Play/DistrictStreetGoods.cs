using System;
using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;
using UnityEngine.Rendering;
using Camera = UnityEngine.Camera;

namespace Suborned.Play
{
    internal sealed class DistrictStreetGoods : IDisposable
    {
        struct Counter
        {
            public Pt at;
            public Vector3 position;
            public Quaternion heading;
            public bool post;
            public int runner;
        }

        readonly Material ink;
        readonly Mesh[] meshes;
        readonly List<Matrix4x4>[] batches = { new(),new(),new(),new() };
        readonly List<Counter> counters = new();
        readonly List<int> nearCounters = new();
        DistrictRun district;
        int lastTick = -1;
        float nextScan;

        public DistrictStreetGoods()
        {
            var sheet=Resources.Load<Texture2D>("StreetLife/HarbourPrint");
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if (!sheet || !shader) return;
            ink=new Material(shader){name="Harbour paper and market goods",enableInstancing=true};
            ink.SetTexture("_BaseMap",sheet);
            ink.SetFloat("_Smoothness",.19f);
            meshes=new[]{DistrictTradeMeshes.Bag(),DistrictTradeMeshes.Newspaper(),DistrictTradeMeshes.PriceCards(),DistrictTradeMeshes.PostArm()};
        }

        void Open(DistrictRun current,SiteFrame frame)
        {
            district=current; counters.Clear(); nearCounters.Clear(); nextScan=0; lastTick=-1;
            foreach (var venue in current.Map.Ground.Venues)
            foreach (var f in venue.Fixtures)
            {
                bool post=f.Kit=="PneumaticPost";
                if (!post && f.Kit!="ProduceStall") continue;
                var pos=frame.World(f.At);
                pos.y=current.Map.Ground.ElevationCm(f.At)*.01f;
                counters.Add(new Counter { at=f.At,position=pos,heading=Quaternion.Euler(0,f.Yaw,0),post=post,runner=-1 });
            }
        }

        public void Carry(StreetPurchase item,Transform hand,Transform body,Camera view)
        {
            if (!ink || item==StreetPurchase.None || !hand) return;
            if (!Prominent(view,body.position,27f)) return;
            // Keep the handles in the palm as the arm swings. Gravity keeps the contents level.
            var facing=body.rotation;
            var grip=hand.position+facing*new Vector3(.025f,-.015f,.013f);
            bool paper=item==StreetPurchase.Newspaper;
            var turn=facing*Quaternion.Euler(paper ? 8 : 0,paper ? -68 : -25,paper ? 12 : 0);
            batches[paper ? 1 : 0].Add(Matrix4x4.TRS(grip,turn,Vector3.one));
        }

        static bool Prominent(Camera view,Vector3 pos,float metres)
        {
            if (view.orthographic) return view.orthographicSize<=metres*.8f;
            return (view.transform.position-pos).sqrMagnitude<=metres*metres;
        }

        public void Draw(DistrictRun current,SiteFrame frame,Camera view,int tick,float alpha)
        {
            if (!ink) return;
            if (district!=current) Open(current,frame);
            bool scan=Time.unscaledTime>=nextScan;
            if (scan)
            {
                nearCounters.Clear();
                for (int i=0;i<counters.Count;i++)
                {
                    var p=counters[i].position+Vector3.up;
                    if (!Prominent(view,p,32)) continue;
                    var v=view.WorldToViewportPoint(p);
                    if (v.z>0 && v.x>-.12f && v.x<1.12f && v.y>-.2f && v.y<1.2f) nearCounters.Add(i);
                }
                nextScan=Time.unscaledTime+.15f;
            }
            foreach (int index in nearCounters)
            {
                var c=counters[index];
                if (!c.post) { batches[2].Add(Matrix4x4.TRS(c.position,c.heading,Vector3.one)); continue; }
                var traffic=current.Traffic;
                if (scan || tick!=lastTick)
                {
                    c.runner=-1;
                    for(int i=0;i<traffic.RobotCount;i++)
                        if (traffic.RobotKind(i)==StreetRobot.CapsuleRunner && G.Within(traffic.Robot(i,tick).At,c.at,190))
                        { c.runner=i; break; }
                    counters[index]=c;
                }
                float lift=0;
                if (c.runner>=0)
                {
                    float before=traffic.ServiceMechanism(c.runner,Math.Max(0,tick-1)).lift;
                    lift=Mathf.Lerp(before,traffic.ServiceMechanism(c.runner,tick).lift,alpha)*.001f;
                }
                var pivot=c.position+c.heading*new Vector3(.276f,1.36f,-.126f);
                batches[3].Add(Matrix4x4.TRS(pivot,c.heading*Quaternion.Euler(0,0,-62f+lift*68f),Vector3.one));
            }
            lastTick=tick;
            var rp=new RenderParams(ink){camera=view,shadowCastingMode=ShadowCastingMode.On,receiveShadows=true,lightProbeUsage=LightProbeUsage.Off};
            for (int i=0;i<batches.Length;i++)
            {
                var batch=batches[i];
                if (batch.Count==0) continue;
                if (SystemInfo.supportsInstancing) Graphics.RenderMeshInstanced(rp,meshes[i],0,batch);
                else foreach(var matrix in batch) Graphics.RenderMesh(rp,meshes[i],0,matrix);
                batch.Clear();
            }
        }

        public void Dispose()
        {
            if (meshes!=null) foreach (var m in meshes) UnityEngine.Object.Destroy(m);
            if (ink) UnityEngine.Object.Destroy(ink);
        }
    }
}
