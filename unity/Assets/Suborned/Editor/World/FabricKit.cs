using UnityEngine;

namespace Suborned.World
{
    /// <summary>The export's `fabric` layer (grammar §2a colour life; Splice's drawing notes, brief 007 round 3), one element per awning,
    /// banner and flag, painted from the facade atlas (every surface an accent role, `canvas` or chrome; never the cone red):
    /// awning: back edge on the wall at (at, z), w wide, sloping d out and fall down to a valance drop, stripes `stripe` wide running
    /// down the slope, alternating stripes[0] / stripes[1], canvas underneath; banner: a w x h panel facing `normal` from z0, hung arm m
    /// out from the wall on two chrome arms, a gold border border.w round it on both faces; flag: a chrome mast h high at at / z with a
    /// gold finial and a pennant (fly x drop) at its top flying toward yaw, with a slight wave.</summary>
    public sealed class FabricKit
    {
        readonly Nodes nodes; readonly Atlas atlas;
        public int Awnings, Banners, Flags;

        public FabricKit(Nodes nodes, Atlas atlas) { this.nodes = nodes; this.atlas = atlas; }

        static string Sw(string surface) => ProcSkin.Swatch(surface);

        public void Build(Transform layer, FabricDto f)
        {
            foreach (var a in f.Awnings) { Part(layer, a.Id, k => Awning(k, a)); Awnings++; }
            foreach (var b in f.Banners) { Part(layer, b.Id, k => Banner(k, b)); Banners++; }
            foreach (var x in f.Flags) { Part(layer, x.Id, k => Flag(k, x)); Flags++; }
            Debug.Log($"World: fabric awnings {Awnings}, banners {Banners}, flags {Flags}");
        }

        void Part(Transform layer, string id, System.Action<Pb> draw)
        {
            var kit = new MeshKit(); draw(new Pb(kit, atlas));
            nodes.Part(Nodes.Element(layer, id), id, kit, atlas.Matte);
        }

        /// <summary>The wall frame centred on `at`, `w` long, its outward normal `n`.</summary>
        static WallFrame Frame(Vector2 at, Vector2 n, float w)
        {
            n = n.normalized; var u = new Vector2(-n.y, n.x);
            var f = new WallFrame(at - u * (w * 0.5f), at + u * (w * 0.5f));
            return Vector2.Dot(f.N, n) >= 0 ? f : new WallFrame(at + u * (w * 0.5f), at - u * (w * 0.5f));
        }

        static void Awning(Pb B, AwningDto a)
        {
            var w = Frame(a.At, a.Normal, a.W);
            float zb = a.Z, zf = a.Z - a.Fall, zv = zf - a.Valance, d0 = 0.03f, d1 = a.D;
            int n = Mathf.Max(1, Mathf.RoundToInt(a.W / Mathf.Max(0.1f, a.Stripe))); float sw = w.Len / n;
            var up = (w.Out * a.Fall + Vector3.up * a.D).normalized; // the cloth's top normal
            for (int i = 0; i < n; i++)
            {
                string st = Sw(a.Stripes[i % 2]);
                float s0 = i * sw, s1 = (i + 1) * sw;
                B.Quad(st, Cls.Matte, w.P(s0, zb, d0), w.P(s1, zb, d0), w.P(s1, zf, d1), w.P(s0, zf, d1), up, 1f, 1f, 0.95f, 0.95f);
                B.Quad(Sw("canvas"), Cls.Matte, w.P(s0, zb - 0.01f, d0), w.P(s1, zb - 0.01f, d0), w.P(s1, zf - 0.01f, d1), w.P(s0, zf - 0.01f, d1), -up, 0.7f, 0.7f, 0.8f, 0.8f);
                // the valance: a straight drop, scalloped at the hem (every stripe dips 0.06 m at its middle)
                float sm = (s0 + s1) * 0.5f;
                B.Quad(st, Cls.Matte, w.P(s0, zf, d1), w.P(s1, zf, d1), w.P(s1, zv, d1), w.P(s0, zv, d1), w.Out, 0.95f, 0.95f, 0.9f, 0.9f);
                B.Tri3(st, Cls.Matte, w.P(s0, zv, d1), w.P(s1, zv, d1), w.P(sm, zv - 0.06f, d1), w.Out, 0.9f);
                B.Quad(st, Cls.Matte, w.P(s0, zf, d1 - 0.01f), w.P(s1, zf, d1 - 0.01f), w.P(s1, zv, d1 - 0.01f), w.P(s0, zv, d1 - 0.01f), -w.Out, 0.75f, 0.75f, 0.7f, 0.7f);
            }
            // side cheeks: the end stripes' triangles under the slope
            string e0 = Sw(a.Stripes[0]), e1 = Sw(a.Stripes[(n - 1) % 2]);
            B.Tri3(e0, Cls.Matte, w.P(0, zb, d0), w.P(0, zf, d1), w.P(0, zf, d0 + 0.02f), -w.Along, 0.85f);
            B.Tri3(e1, Cls.Matte, w.P(w.Len, zb, d0), w.P(w.Len, zf, d1), w.P(w.Len, zf, d0 + 0.02f), w.Along, 0.85f);
            // chrome rod along the back edge
            B.Box("chrome", Cls.Chrome, w, -0.03f, w.Len + 0.03f, zb - 0.04f, zb + 0.04f, 0f, 0.08f);
        }

        static void Banner(Pb B, BannerDto b)
        {
            var w = Frame(b.At, b.Normal, b.W);
            float z0 = b.Z0, z1 = b.Z0 + b.H, d = b.Arm, t = 0.03f, bw = b.BorderW;
            string face = Sw(b.Surface), gold = Sw(b.BorderSurface);
            B.Box(face, Cls.Matte, w, 0f, w.Len, z0, z1, d - t, d + t, 0f, null, true);
            B.Quad(face, Cls.Matte, w.P(0, z0, d - t), w.P(w.Len, z0, d - t), w.P(w.Len, z1, d - t), w.P(0, z1, d - t), -w.Out, 0.85f, 0.85f, 0.9f, 0.9f);
            foreach (float side in new[] { 1f, -1f })
            {   // the gold border on both faces
                float dd = d + side * (t + 0.004f), de = dd + side * 0.02f;
                float lo = Mathf.Min(dd, de), hi = Mathf.Max(dd, de);
                B.Box(gold, Cls.Brass, w, 0f, w.Len, z1 - bw, z1, lo, hi);
                B.Box(gold, Cls.Brass, w, 0f, w.Len, z0, z0 + bw, lo, hi);
                B.Box(gold, Cls.Brass, w, 0f, bw, z0 + bw, z1 - bw, lo, hi);
                B.Box(gold, Cls.Brass, w, w.Len - bw, w.Len, z0 + bw, z1 - bw, lo, hi);
            }
            // the arms: chrome rods from the wall to the banner's top and bottom, a gold ball at each top end
            foreach (float s in new[] { 0.06f, w.Len - 0.06f })
            {
                B.Box("chrome", Cls.Chrome, w, s - 0.035f, s + 0.035f, z1 - 0.07f, z1 + 0.0f, 0f, d + 0.05f);
                B.Box("chrome", Cls.Chrome, w, s - 0.035f, s + 0.035f, z0, z0 + 0.07f, 0f, d + 0.05f);
                B.Box(gold, Cls.Brass, w, s - 0.07f, s + 0.07f, z1 - 0.1f, z1 + 0.06f, d + 0.04f, d + 0.16f);
            }
        }

        static void Flag(Pb B, FlagDto f)
        {
            // the mast: an octagonal chrome prism, a gold finial
            const int N = 8; float r = 0.07f, z0 = f.Z, z1 = f.Z + f.H;
            string mast = f.Mast == "chrome" ? "chrome" : Sw(f.Mast);
            for (int i = 0; i < N; i++)
            {
                float a0 = 2 * Mathf.PI * i / N, a1 = 2 * Mathf.PI * (i + 1) / N;
                var p0 = new Vector3(f.At.x + Mathf.Cos(a0) * r, 0, f.At.y + Mathf.Sin(a0) * r);
                var p1 = new Vector3(f.At.x + Mathf.Cos(a1) * r, 0, f.At.y + Mathf.Sin(a1) * r);
                var o = new Vector3(Mathf.Cos((a0 + a1) * 0.5f), 0, Mathf.Sin((a0 + a1) * 0.5f));
                B.Quad(mast, Cls.Chrome, p0 + Vector3.up * z0, p0 + Vector3.up * z1, p1 + Vector3.up * z1, p1 + Vector3.up * z0, o);
            }
            var mw = Frame(f.At, Vector2.right, 0.3f);
            B.Box(Sw("accent_gold"), Cls.Brass, mw, 0f, 0.3f, z1, z1 + 0.22f, -0.11f, 0.11f);
            B.Box(mast, Cls.Chrome, mw, 0.05f, 0.25f, z0, z0 + 0.35f, -0.12f, 0.12f); // the foot
            // the pennant: hoist on the mast below the finial, flying toward yaw with a slight wave
            var fly = new Vector2(Mathf.Cos(f.Yaw), Mathf.Sin(f.Yaw)); var side = new Vector2(-fly.y, fly.x);
            float top = z1 - 0.1f, bot = top - f.PennantH; const int S = 4; string cloth = Sw(f.Surface);
            Vector3 At(float u, float z) { var q = f.At + fly * (r + u * f.PennantW) + side * (Mathf.Sin(u * Mathf.PI * 1.5f) * 0.12f * u); return new Vector3(q.x, z, q.y); }
            var n3 = new Vector3(side.x, 0, side.y);
            for (int i = 0; i < S; i++)
            {
                float u0 = i / (float)S, u1 = (i + 1) / (float)S;
                B.Quad(cloth, Cls.Matte, At(u0, bot), At(u0, top), At(u1, top), At(u1, bot), n3, 0.9f, 1f, 1f, 0.9f);
                B.Quad(cloth, Cls.Matte, At(u0, bot), At(u0, top), At(u1, top), At(u1, bot), -n3, 0.8f, 0.9f, 0.9f, 0.8f);
            }
        }
    }
}
