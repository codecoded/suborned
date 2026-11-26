using UnityEngine;

namespace Suborned.World.Play
{
    sealed partial class DistrictStreetArchitecture
    {
        sealed partial class Surface
        {
            // Closed square sections for the fine rails, brackets and cantilever arms. No separate renderers.
            public void Beam(int mat, Vector3 a, Vector3 b, float width, float depth)
            {
                var axis = (b - a).normalized;
                var across = Vector3.Cross(axis, Mathf.Abs(axis.y) > .98f ? Vector3.forward : Vector3.up).normalized;
                var height = Vector3.Cross(across, axis) * (depth * .5f);
                across *= width * .5f;
                var ring = new[] { -across - height, across - height, across + height, -across + height };
                for (int i = 0; i < 4; i++)
                {
                    var u = ring[i]; var v = ring[(i + 1) % 4];
                    Face(mat, a + u, b + u, b + v, a + v);
                }
                Face(mat, a + ring[0], a + ring[1], a + ring[2], a + ring[3]);
                Face(mat, b + ring[3], b + ring[2], b + ring[1], b + ring[0]);
            }

            public void Canvas(int fabric, int stripe, float x, float y, float z, float width, float reach, bool near)
            {
                int bands = 2 * Mathf.RoundToInt(width / 1.15f) + 1;
                int steps = near ? 4 : 2;
                Vector3 Edge(float at, int step)
                {
                    float t = (float)step / steps;
                    return new Vector3(at, y - reach * (.10f * t + .31f * t * t), z + reach * t);
                }
                for (int n = 0; n < bands; n++)
                {
                    float left = x + width * ((float)n / bands - .5f), right = left + width / bands;
                    int colour = n % 2 == 0 ? fabric : stripe;
                    for (int k = 0; k < steps; k++)
                    {
                        var a = Edge(left,k); var b = Edge(right,k); var c = Edge(right,k+1); var d = Edge(left,k+1);
                        Face(colour, a, d, c, b); Face(colour, b, c, d, a);
                    }
                    var l = Edge(left,steps); var r = Edge(right,steps); var m = (l+r)*.5f;
                    var hem = Vector3.down * .13f; var dip = Vector3.down * .11f;
                    Face(colour,l,m,m+hem+dip,l+hem); Face(colour,m,r,r+hem,m+hem+dip);
                    Face(colour,l+hem,m+hem+dip,m,l); Face(colour,m+hem+dip,r+hem,r,m);
                }
            }

            public void Sign(int material, Vector3 centre, float width, float height, int tile, bool reverse = false)
            {
                float u = (tile % 2) * .5f, v = .75f - (tile / 2) * .25f;
                float lo = width * (reverse ? .5f : -.5f), hi = -lo;
                int i = vertices.Count;
                foreach (var offset in new[] { new Vector3(lo,-height/2), new Vector3(hi,-height/2), new Vector3(hi,height/2), new Vector3(lo,height/2) })
                    vertices.Add(centre + offset);
                uv.AddRange(new[] { new Vector2(u+.004f,v+.005f),new Vector2(u+.496f,v+.005f),new Vector2(u+.496f,v+.245f),new Vector2(u+.004f,v+.245f) });
                indices[material].AddRange(new[] { i,i+1,i+2,i,i+2,i+3 });
            }
        }

        static void UpperLoggia(Surface s, float w, int variant, bool near)
        {
            float width = w * (variant % 2 == 0 ? .75f : .59f);
            float x = (w - width) * (variant < 2 ? -.38f : .38f);
            // The retained podium tops out at 8.23 m. The bay now has a shaded second floor and an open roof terrace.
            s.Solid(1, x, .17f, width, .31f, 6.04f, 1.82f, .11f);
            s.Solid(0, x, .64f, width+.42f, 1.54f, 5.98f, .26f, .58f);
            s.Solid(2, x, .68f, width+.46f, 1.55f, 6.24f, .09f, .59f);
            s.Solid(0, x, .48f, width+.52f, 1.17f, 7.91f, .24f, .44f);
            if (variant != 3) s.Canvas(8,9,x,7.86f,.33f,width*.86f,1.30f,near);
            s.Beam(3,new Vector3(x-width*.47f,7.08f,1.24f),new Vector3(x+width*.47f,7.08f,1.24f),.07f,.08f);
            for (int i=0;i<=4;i++)
            {
                float post=x+width*(i*.235f-.47f);
                s.Beam(3,new Vector3(post,6.28f,1.24f),new Vector3(post,7.08f,1.24f),.052f,.055f);
            }
            float boxX=x+width*(variant%2 == 0 ? -.23f : .20f);
            s.Solid(0,boxX,.92f,width*.41f,.73f,6.28f,.38f,.25f);
            s.TerracePlants(5,new Vector3(boxX,6.68f,1.0f),width*.37f,variant+11,near);
            if (near && variant != 3) s.Canvas(8,9,-x,3.47f,.42f,w*.52f,1.17f,true);
        }
    }
}
