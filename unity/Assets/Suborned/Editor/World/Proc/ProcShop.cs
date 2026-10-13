using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Procedural shopfronts and the sign grammar: recessed glazing with chrome mullions, tiled bulkhead, enamel piers
    /// and header, a canopy with a chosen profile, and a sign (arrow, starburst, boomerang, disc, pylon, blade, pennant, bar,
    /// plaque) with a trade icon and a neon tube outline, in the trade's colourway.</summary>
    public sealed partial class ShopKit
    {
        void Front(Transform layer, FrontageDto f)
        {
            var el = Nodes.Element(layer, f.Id);
            var h = fk.Resolve(f.Host);
            var w = new WallFrame(f.A, f.B);
            if (Vector2.Dot(w.N, f.Normal) < 0) w = new WallFrame(f.B, f.A);
            bool civic = f.Lobby || f.Trade == "lobby";
            string[] tc = spec.Trades.TryGetValue(f.Trade ?? "", out var t) ? t : spec.TradeDefault;
            string trade = civic ? h.A : tc[0], stripe = civic ? h.B : tc[1];
            float W = w.Len, z0 = f.Z, H = spec.ShopHeight;
            var kit = new MeshKit(); var B = new Pb(kit, spec.Atlas);
            int D = ProcKit.Detail((f.A + f.B) * 0.5f);
            var d = new Dice(f.Id, 11);
            const float fd = 0.62f;
            float bv = D >= 2 ? 0.05f : 0f;
            // grammar §2a.1 on a skinned host (not lobbies): the bulkhead in the family's plinth faience up to plinth + bulkhead, and the
            // piers between shop bays in the building's accent (half a pier each side of the tenancy line)
            var sk = civic ? null : ProcSkin.OfBuilding(f.Host);
            float pw = sk != null && sk.FaiencePiers ? sk.FaiencePier * 0.5f : 0.4f;
            float bt = sk != null ? z0 + sk.FaiencePlinth + sk.FaienceBulkhead : z0 + 1.0f; // bulkhead top
            string pier = sk != null && sk.FaiencePiers ? ProcSkin.Swatch(sk.Accent) : trade;

            // A kit facade piece stands over this tenancy (f.Facade): the piece carries its own shopfront and canopy, so the shell is
            // laid out (its canopy fixes the sign's height) but not drawn; the sign stays (brief 007 E).
            var S = f.Facade == null ? B : new Pb(new MeshKit(), spec.Atlas);
            S.Box(h.Plinth, Cls.Stone, w, 0, W, z0, z0 + 0.55f, 0f, 0.7f, bv);
            S.Box(pier, Cls.Enamel, w, 0, pw, z0 + 0.55f, z0 + H, 0f, fd, bv);
            S.Box(pier, Cls.Enamel, w, W - pw, W, z0 + 0.55f, z0 + H, 0f, fd, bv);
            S.Box(trade, Cls.Enamel, w, pw, W - pw, z0 + 3.6f, z0 + H, 0f, fd, bv, null, true);
            // bulkhead (mosaic enamel; faience on a skinned host), glazing set back, reveals
            S.R(civic ? "white" : sk != null ? h.Plinth : stripe, Cls.Enamel, w, pw, W - pw, z0 + 0.55f, bt, 0.44f, 0.8f, 1f);
            if (D >= 1) S.Box("chrome", Cls.Chrome, w, pw, W - pw, bt, bt + 0.06f, 0.26f, 0.5f);
            string gl = spec.Glass[d.Int(spec.Glass.Length)];
            S.R(gl, Cls.Glass, w, pw, W - pw, bt + 0.06f, z0 + 3.6f, 0.26f, 0.8f, 0.92f);
            if (D >= 1)
            {
                S.Quad(pier, Cls.Enamel, w.P(pw, bt, 0.26f), w.P(pw, z0 + 3.6f, 0.26f), w.P(pw, z0 + 3.6f, fd), w.P(pw, bt, fd), w.Along, 0.5f, 0.5f, 0.85f, 0.85f);
                S.Quad(pier, Cls.Enamel, w.P(W - pw, bt, 0.26f), w.P(W - pw, z0 + 3.6f, 0.26f), w.P(W - pw, z0 + 3.6f, fd), w.P(W - pw, bt, fd), -w.Along, 0.5f, 0.5f, 0.85f, 0.85f);
            }
            float sd = f.Entrance != null ? w.S(f.Entrance.Centre) : W * 0.5f;
            int nm = Mathf.Max(1, Mathf.RoundToInt((W - 0.8f) / 1.5f));
            if (D >= 2)
                for (int i = 1; i < nm; i++)
                {
                    float s = pw + (W - 2 * pw) * i / nm;
                    if (Mathf.Abs(s - sd) < 1.4f) continue;
                    S.Box("chrome", Cls.Chrome, w, s - 0.035f, s + 0.035f, z0 + 1.06f, z0 + 3.6f, 0.26f, 0.34f);
                }
            // entrance: chrome frame, dark glass door, bar handle
            S.Box("chrome", Cls.Chrome, w, sd - 1.15f, sd - 1.05f, z0, z0 + 2.75f, 0.24f, 0.34f);
            S.Box("chrome", Cls.Chrome, w, sd + 1.05f, sd + 1.15f, z0, z0 + 2.75f, 0.24f, 0.34f);
            S.Box("chrome", Cls.Chrome, w, sd - 1.15f, sd + 1.15f, z0 + 2.65f, z0 + 2.75f, 0.24f, 0.34f);
            S.R("glass_dark", Cls.Glass, w, sd - 1.05f, sd + 1.05f, z0, z0 + 2.65f, 0.2f, 0.7f, 0.9f);
            if (D >= 2)
            {
                S.Box("chrome", Cls.Chrome, w, sd - 0.03f, sd + 0.03f, z0, z0 + 2.65f, 0.2f, 0.26f);
                S.Box("chrome", Cls.Chrome, w, sd - 0.55f, sd - 0.47f, z0 + 0.9f, z0 + 1.7f, 0.26f, 0.32f);
                S.Box("chrome", Cls.Chrome, w, sd + 0.47f, sd + 0.55f, z0 + 0.9f, z0 + 1.7f, 0.26f, 0.32f);
            }

            float ct = z0 + 3.8f, c1 = 2.4f;
            if (f.Canopy != null)
            {
                var cp = f.Canopy; float c0 = w.D(cp.Centre) - cp.Size.y * 0.5f; c1 = w.D(cp.Centre) + cp.Size.y * 0.5f;
                float cz = cp.Base; ct = cp.Base + cp.Size.z;
                string cs = trade;
                switch (d.Int(3))
                {
                    case 0: S.Box(cs, Cls.Enamel, w, 0, W, cz, ct, c0, c1, D >= 2 ? 0.07f : 0f, null, true); break;
                    case 1:
                        S.Extrude(cs, Cls.Enamel, w, 0, W, new[] { new Vector2(c0, cz), new Vector2(c1, cz), new Vector2(c1, ct + 0.2f), new Vector2(c1 - 0.12f, ct + 0.2f), new Vector2(c1 - 0.12f, ct), new Vector2(c0, ct) });
                        break;
                    default:
                        S.Extrude(cs, Cls.Enamel, w, 0, W, new[] { new Vector2(c0, cz), new Vector2(c1 - 0.28f, cz), new Vector2(c1 - 0.08f, cz + 0.05f), new Vector2(c1, cz + 0.13f), new Vector2(c1, ct - 0.04f), new Vector2(c1 - 0.1f, ct), new Vector2(c0, ct) });
                        break;
                }
                if (D >= 1) S.Box(civic ? stripe : trade, Cls.Enamel, w, 0, W, cz - 0.36f, cz, c1 - 0.08f, c1, 0.03f);
                if (sk != null && D >= 1) S.Box(ProcSkin.Swatch(sk.Trim), Cls.Brass, w, 0, W, cz + 0.04f, cz + 0.14f, c1, c1 + 0.05f); // §2a.5 the marquee's gold edge
                else if (D >= 2) S.Box("chrome", Cls.Chrome, w, 0, W, cz + 0.05f, cz + 0.1f, c1, c1 + 0.05f);
                if (!civic)
                {
                    int ns = Mathf.Max(2, Mathf.RoundToInt(W / 0.75f)); float sw = W / ns;
                    for (int i = 1; i < ns; i += 2)
                        S.Quad(stripe, Cls.Enamel, w.P(i * sw, ct + 0.01f, c0), w.P(i * sw, ct + 0.01f, c1 - 0.14f), w.P((i + 1) * sw, ct + 0.01f, c1 - 0.14f), w.P((i + 1) * sw, ct + 0.01f, c0), Vector3.up);
                }
            }
            if (f.Service != null)
            {
                var sv = f.Service; float s0 = w.S(sv.Centre) - sv.Size.x * 0.5f, s1 = w.S(sv.Centre) + sv.Size.x * 0.5f;
                S.Box("service", Cls.Matte, w, s0, s1, sv.Base, sv.Base + sv.Size.z, w.D(sv.Centre) - sv.Size.y * 0.5f, w.D(sv.Centre) + sv.Size.y * 0.5f, bv);
            }
            int v1 = kit.VertexCount;
            if (f.Sign != null && ProcGrammar.Signs.TryGetValue(f.Id, out var ss)) ProcSign(B, w, f, ss, W, ct, c1, z0, D);
            ProcKit.Tally("sign", D, v1, kit.VertexCount); ProcKit.Tally("shopfront", D, 0, v1);
            nodes.Part(el, "front", kit, spec.Atlas.Matte);
        }

        // ---------------------------------------------------------------- sign grammar

        static readonly Dictionary<string, Vector2[]> Shapes = new()
        {
            ["bar"] = Pp(-.5f, -.36f, -.44f, -.5f, .44f, -.5f, .5f, -.36f, .5f, .36f, .44f, .5f, -.44f, .5f, -.5f, .36f),
            ["arrow"] = Pp(-.5f, .26f, .08f, .26f, .08f, .5f, .5f, 0, .08f, -.5f, .08f, -.26f, -.5f, -.26f),
            ["boomerang"] = Pp(-.5f, .5f, -.16f, .5f, 0, .06f, .16f, .5f, .5f, .5f, .14f, -.5f, -.14f, -.5f),
            ["pennant"] = Pp(-.5f, .5f, .5f, .02f, -.5f, -.5f),
            ["pylon"] = Pp(-.5f, -.5f, .5f, -.5f, .5f, .3f, 0, .5f, -.5f, .3f),
            ["blade"] = Pp(-.4f, -.5f, .4f, -.5f, .5f, -.34f, .5f, .34f, .4f, .5f, -.4f, .5f, -.5f, .34f, -.5f, -.34f),
            ["plaque"] = Pp(-.5f, -.4f, -.4f, -.5f, .4f, -.5f, .5f, -.4f, .5f, .4f, .4f, .5f, -.4f, .5f, -.5f, .4f),
        };

        static Vector2[] Pp(params float[] xy) { var l = new Vector2[xy.Length / 2]; for (int i = 0; i < l.Length; i++) l[i] = new Vector2(xy[2 * i], xy[2 * i + 1]); return l; }

        static Vector2[] Star(int n, float ro, float ri)
        {
            var l = new Vector2[2 * n];
            for (int i = 0; i < 2 * n; i++) { float a = Mathf.PI * i / n + Mathf.PI * 0.5f, r = i % 2 == 0 ? ro : ri; l[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r); }
            return l;
        }

        static Vector2[] Disc(int n, float r) { var l = new Vector2[n]; for (int i = 0; i < n; i++) { float a = 2 * Mathf.PI * i / n; l[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; } return l; }

        void ProcSign(Pb B, in WallFrame w, FrontageDto f, SignSpec ss, float W, float ct, float c1, float z0, int D)
        {
            var wl = w; var sg = f.Sign; float sc = Mathf.Clamp(w.S(sg.Centre), 1.2f, W - 1.2f);
            Vector2[] shape; float sw, sh, zc, dd = Mathf.Max(0.35f, c1 - 0.4f), s = sc; bool blade = ss.Mount == "blade";
            var d = new Dice(f.Id, 29); bool flip = d.Int(2) == 0;
            switch (ss.Shape)
            {
                case "starburst": shape = Star(12, .5f, .3f); sw = sh = 2.6f; zc = ct + 1.55f; break;
                case "disc": shape = Disc(16, .5f); sw = sh = 2.1f; zc = ct + 1.4f; break;
                case "pylon": shape = Shapes["pylon"]; sw = 0.95f; sh = 4.4f; zc = ct + 2.4f; s = flip ? Mathf.Min(W - 1.0f, W * 0.86f) : Mathf.Max(1.0f, W * 0.14f); break;
                case "boomerang": shape = Shapes["boomerang"]; sw = 3.2f; sh = 1.8f; zc = ct + 1.25f; break;
                case "pennant": shape = Shapes["pennant"]; if (flip) shape = shape.Select(p => new Vector2(-p.x, p.y)).ToArray(); sw = 2.5f; sh = 1.7f; zc = ct + 1.2f; break;
                case "arrow": shape = Shapes["arrow"]; if (flip) shape = shape.Select(p => new Vector2(-p.x, p.y)).ToArray(); sw = Mathf.Min(3.8f, W - 0.8f); sh = 1.15f; zc = sg.Base + 0.6f; dd = 0.2f; break;
                case "blade": shape = Shapes["blade"]; sw = 1.7f; sh = 1.5f; zc = ct + 1.0f; break;
                case "plaque": shape = Shapes["plaque"]; sw = 1.2f; sh = 1.2f; zc = sg.Base + 0.6f; dd = 0.2f; break;
                default: shape = Shapes["bar"]; sw = Mathf.Min(sg.Size.x, W - 0.6f); sh = 1.1f; zc = sg.Base + 0.6f; dd = 0.2f; break;
            }
            var pts = shape.Select(p => new Vector2(p.x * sw, p.y * sh)).ToList();
            System.Func<Vector2, Vector3> map; Vector3 nrm;
            if (blade)
            {   // perpendicular to the wall, hung from a chrome arm
                float bs = flip ? Mathf.Max(0.8f, W * 0.1f) : Mathf.Min(W - 0.8f, W * 0.9f);
                map = q => wl.P(bs, zc + q.y, 1.2f + q.x); nrm = w.Along;
                B.Box("chrome", Cls.Chrome, w, bs - 0.04f, bs + 0.04f, zc + sh * 0.5f - 0.05f, zc + sh * 0.5f + 0.03f, 0f, 2.1f);
                s = bs;
            }
            else { float s2 = s; map = q => wl.P(s2 + q.x, zc + q.y, dd); nrm = w.Out; }
            const float th = 0.22f;
            string ink = spec.Atlas.Colour(ss.Board).grayscale < 0.45f ? "paper" : "ink";
            B.Plate(ss.Board, Cls.Enamel, pts, map, nrm, th);
            if (ss.Mount == "stand" && !blade && D >= 1) B.Box("chrome", Cls.Chrome, w, s - 0.05f, s + 0.05f, ct, zc - sh * 0.35f, dd - 0.05f, dd + 0.05f);
            if (D >= 2) B.Tube(ss.Tube, Cls.Neon, pts, map, nrm, th * 0.5f + 0.012f, 0.08f, 0.07f);
            // trade icon on the board
            var prims = IconOf(ss.Icon);
            float k = Mathf.Min(sw, sh) * (ss.Shape == "bar" || ss.Shape == "arrow" ? 0.8f : 0.5f);
            float ox = ss.Shape == "bar" ? -sw * 0.28f : ss.Shape == "arrow" ? (flip ? 0.5f : -0.5f) * sw * 0.25f : 0f;
            float oy = ss.Shape == "pylon" ? sh * 0.12f : 0f;
            if (D >= 2)
                foreach (var p in prims)
                {
                    var q = p.Pts.Select(v => new Vector2(ox + v.x * k, oy + v.y * k)).ToList();
                    var o = nrm * (th * 0.5f + (p.Hole ? 0.03f : 0.02f));
                    var v3 = q.Select(v => map(v) + o).ToList();
                    B.Fan(p.Hole ? ss.Board : ink, Cls.Matte, v3, nrm);
                }
            if (ss.Shape == "bar" && D >= 2 && sw > 3.2f)
            {   // fascia bar carries two more tube bars as the grammar's trade stripe
                var stripe = new List<Vector2> { new(-sw * 0.12f, -sh * 0.2f), new(sw * 0.42f, -sh * 0.2f), new(sw * 0.42f, sh * 0.2f), new(-sw * 0.12f, sh * 0.2f) };
                B.Plate(ss.Tube, Cls.Neon, stripe, map, nrm, th + 0.04f, false, 1f);
            }
        }
    }
}
