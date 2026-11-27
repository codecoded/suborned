using System.Collections.Generic;
using UnityEngine;

namespace Suborned.Play
{
    // Small opaque meshes share one sheet of market printing, newsprint, fruit and brass.
    // Local front is -Z, in the same metre compass as the stock counter and post.
    internal sealed class DistrictTradeMeshes
    {
        readonly List<Vector3> points = new();
        readonly List<Vector2> uv = new();
        readonly List<int> faces = new();
        static Vector3 P(float x,float y,float z) => new Vector3(x,y,z);
        static Vector2 Ink(int tile,float u,float v) =>
            new Vector2((tile%4+.014f+.972f*u)/4,(3-tile/4+.014f+.972f*v)/4);

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int tile, float u0=0,float u1=1)
        {
            int k=points.Count;
            points.Add(a); points.Add(b); points.Add(c); points.Add(d);
            uv.Add(Ink(tile,u0,0)); uv.Add(Ink(tile,u1,0)); uv.Add(Ink(tile,u1,1)); uv.Add(Ink(tile,u0,1));
            faces.Add(k); faces.Add(k+2); faces.Add(k+1); faces.Add(k); faces.Add(k+3); faces.Add(k+2);
        }

        void Rod(Vector3 a,Vector3 b,float radius,int tile,int sides=6)
        {
            var along=(b-a).normalized;
            var x=Vector3.Cross(along,Mathf.Abs(along.y)<.85f ? Vector3.up : Vector3.forward).normalized*radius;
            var y=Vector3.Cross(along,x);
            for(int i=0;i<sides;i++)
            {
                float t=i*Mathf.PI*2/sides, n=(i+1)*Mathf.PI*2/sides;
                var near=x*Mathf.Cos(t)+y*Mathf.Sin(t); var next=x*Mathf.Cos(n)+y*Mathf.Sin(n);
                Quad(a+next,a+near,b+near,b+next,tile);
            }
        }

        void Fruit(Vector3 centre,Vector3 scale,int tile)
        {
            const int around=9, levels=5;
            Vector3 At(int i,int j)
            {
                float phi=Mathf.Lerp(-1.54f,1.54f,(float)j/levels),theta=i*2*Mathf.PI/around;
                return centre+Vector3.Scale(scale,P(Mathf.Cos(theta)*Mathf.Cos(phi),Mathf.Sin(phi),Mathf.Sin(theta)*Mathf.Cos(phi)));
            }
            for(int j=0;j<levels;j++)
            for(int i=0;i<around;i++) Quad(At(i,j),At(i+1,j),At(i+1,j+1),At(i,j+1),tile);
        }

        Mesh Finish(string name)
        {
            var m=new Mesh { name=name };
            m.SetVertices(points); m.SetUVs(0,uv); m.SetTriangles(faces,0);
            m.RecalculateNormals(); m.RecalculateBounds(); m.UploadMeshData(true);
            return m;
        }

        public static Mesh Bag()
        {
            var m=new DistrictTradeMeshes();
            // The handles meet the hand at the origin, while the bag stays vertical under its own weight.
            var a=P(-.132f,-.383f,-.064f); var b=P(.132f,-.383f,-.064f);
            var c=P(.149f,-.112f,-.077f); var d=P(-.148f,-.116f,-.077f);
            var e=P(-.132f,-.383f,.064f); var f=P(.132f,-.383f,.064f);
            var g=P(.144f,-.121f,.078f); var h=P(-.147f,-.109f,.076f);
            m.Quad(a,b,c,d,0); m.Quad(f,e,h,g,0); m.Quad(e,f,b,a,1);
            var l=P(-.125f,-.137f,.003f); var r=P(.123f,-.132f,.002f);
            m.Quad(d,l,e,a,1); m.Quad(l,h,e,e,1); m.Quad(b,f,r,c,1); m.Quad(r,f,g,g,1);
            m.Quad(d,c,g,h,10);
            m.Fruit(P(-.070f,-.116f,-.013f),P(.065f,.057f,.059f),11);
            m.Fruit(P(.048f,-.112f,-.023f),P(.053f,.058f,.053f),11);
            m.Fruit(P(.014f,-.090f,.024f),P(.046f,.051f,.044f),14);
            m.Rod(P(.082f,-.115f,.037f),P(.104f,-.020f,.058f),.005f,12);
            var leaf=P(.15f,-.021f,.068f);
            m.Quad(P(.091f,-.077f,.051f),P(.100f,-.034f,.028f),leaf,P(.129f,-.061f,.065f),12);
            m.Quad(P(.129f,-.061f,.065f),leaf,P(.100f,-.034f,.028f),P(.091f,-.077f,.051f),12);
            foreach(int side in new[]{-1,1})
            {
                var z=side*.067f;
                var handle=new[]{P(-.064f,-.129f,z),P(-.058f,-.066f,z*.8f),P(-.038f,-.016f,z*.32f),
                    P(-.018f,-.001f,z*.14f),P(.026f,-.001f,z*.14f),P(.043f,-.020f,z*.32f),
                    P(.063f,-.068f,z*.8f),P(.065f,-.129f,z)};
                for(int i=1;i<handle.Length;i++) m.Rod(handle[i-1],handle[i],.0045f,10);
            }
            return m.Finish("Harbour market • filled grocer's bag");
        }

        public static Mesh Newspaper()
        {
            var m=new DistrictTradeMeshes();
            // The uncreased edge fans out. The front page runs continuously across the shallow fold.
            var a=P(-.122f,-.255f,-.016f); var b=P(.015f,-.260f,-.038f); var c=P(.133f,-.249f,-.019f);
            var d=P(-.122f,.016f,.003f); var e=P(.014f,.022f,-.017f); var f=P(.133f,.011f,.003f);
            m.Quad(a,b,e,d,4,0,.54f); m.Quad(b,c,f,e,4,.54f,1);
            var off=P(0,0,.010f);
            m.Quad(c+off,a+off,d+off,f+off,5); m.Quad(a+off,c+off,c,a,8);
            m.Quad(d,e,e+off,d+off,8); m.Quad(e,f,f+off,e+off,8);
            m.Quad(a,d,d+off,a+off,8); m.Quad(c+off,f+off,f,c,8);
            return m.Finish("Harbour Evening Post • folded edition");
        }

        void Board(Vector3 at,float width,float height,int tile)
        {
            var x=Vector3.right*width*.5f;
            var up=P(0,height,.030f);
            var thick=P(0,0,.012f);
            Quad(at-x,at+x,at+x+up,at-x+up,tile);
            Quad(at+x+thick,at-x+thick,at-x+up+thick,at+x+up+thick,8);
            Quad(at-x+up,at+x+up,at+x+up+thick,at-x+up+thick,13);
        }

        public static Mesh PriceCards()
        {
            var m=new DistrictTradeMeshes();
            for(int i=0;i<2;i++)
            {
                float x=i==0 ? -.82f : .03f;
                var basePoint=P(x,.92f,-.42f);
                m.Rod(basePoint,basePoint+P(0,.25f,.024f),.009f,13,8);
                m.Board(basePoint+P(0,.12f,-.012f),.265f,.157f,i+2);
            }
            return m.Finish("Harbour market • enamel price tickets");
        }

        public static Mesh PostArm()
        {
            var m=new DistrictTradeMeshes();
            // Bolted to the shoulder of the column, the collector's paddle turns about the brass pin.
            m.Rod(P(-.027f,0,0),P(.051f,0,0),.026f,13,10);
            m.Rod(P(.023f,0,0),P(.184f,0,0),.012f,13,8);
            m.Board(P(.176f,-.043f,-.006f),.170f,.102f,7);
            return m.Finish("Harbour post • collection paddle");
        }
    }
}
