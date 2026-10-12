using System.Collections.Generic;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Shopfronts (ProcShop: recessed glazing, enamel piers, canopy profiles, the sign grammar) and the trade icons their
    /// signs carry, parsed from facade.json. Lobbies and civic doors take the building's enamel instead of a trade colour.
    /// Geometry sizes and positions come from the export's frontage prims.</summary>
    public sealed partial class ShopKit
    {
        readonly Nodes nodes; readonly FacadeKit fk; readonly FacadeSpec spec;
        readonly Dictionary<string, List<Prim>> icons = new();

        public ShopKit(Nodes nodes, FacadeKit fk) { this.nodes = nodes; this.fk = fk; spec = fk.Spec; }

        public void Frontages(Transform layer, List<FrontageDto> list) { foreach (var f in list) Front(layer, f); }

        // ---------------------------------------------------------------- icons (unit square, centre 0)

        sealed class Prim { public List<Vector2> Pts; public bool Hole; }

        static List<Vector2> Circle(float cx, float cy, float rx, float ry, int n = 12)
        {
            var l = new List<Vector2>(n);
            for (int i = 0; i < n; i++) { float a = 2 * Mathf.PI * i / n; l.Add(new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry)); }
            return l;
        }

        static List<Vector2> P(params float[] xy) { var l = new List<Vector2>(); for (int i = 0; i < xy.Length; i += 2) l.Add(new Vector2(xy[i], xy[i + 1])); return l; }

        List<Prim> IconOf(string name)
        {
            if (name != null && icons.TryGetValue(name, out var cached)) return cached;
            var list = new List<Prim>();
            var src = name != null && spec.Icons.TryGetValue(name, out var s) ? s : "d 0 0 .8 .8";
            foreach (var part in src.Split(';'))
            {
                var tk = part.Trim().Split(' ');
                if (tk.Length < 5) continue;
                string kind = tk[0]; bool hole = kind.Length > 1 && kind[1] == 'h';
                float cx = F(tk[1]), cy = F(tk[2]), pw = F(tk[3]), ph = F(tk[4]), rot = tk.Length > 5 ? F(tk[5]) * Mathf.Deg2Rad : 0f;
                List<Vector2> pts = kind[0] switch
                {
                    'd' => Circle(0, 0, pw * .5f, ph * .5f, 10),
                    't' => P(-pw * .5f, -ph * .5f, pw * .5f, -ph * .5f, 0, ph * .5f),
                    'v' => P(-pw * .5f, ph * .5f, pw * .5f, ph * .5f, 0, -ph * .5f),
                    'l' => P(0, ph * .5f, pw * .5f, 0, 0, -ph * .5f, -pw * .5f, 0),
                    _ => P(-pw * .5f, -ph * .5f, pw * .5f, -ph * .5f, pw * .5f, ph * .5f, -pw * .5f, ph * .5f),
                };
                float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
                for (int i = 0; i < pts.Count; i++) pts[i] = new Vector2(cx + pts[i].x * cs - pts[i].y * sn, cy + pts[i].x * sn + pts[i].y * cs);
                list.Add(new Prim { Pts = pts, Hole = hole });
            }
            if (name != null) icons[name] = list;
            return list;
        }

        static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    }
}
