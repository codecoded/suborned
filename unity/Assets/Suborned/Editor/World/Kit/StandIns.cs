using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Suborned.World
{
    /// <summary>Kit pieces standing in for whole procedural elements:
    /// a plan tower site's regenerated piece (export `signatures`, kind tower, id tower/&lt;site&gt;) covers the masses tower/&lt;site&gt;/lower and
    /// /upper and its roof (the podium stays procedural); the signatures layer places the piece (brief 007 E; before, this class placed it
    /// from the kit's `site:` tag on the lower mass).
    /// `landmark:&lt;name&gt;` (a generated piece's kit.json `tags.use`) = the megastructure mega/&lt;name&gt; (its parts and prims; frontages and
    /// connectors stay), standing on the ground at its parts' centre. Yaw 0: the generator builds in the plan frame (+X east, +Z north).</summary>
    public sealed class StandIns
    {
        readonly KitPlacer placer;
        readonly Dictionary<string, string> towers = new(System.StringComparer.Ordinal); // host tower/<site> → kit id
        readonly Dictionary<string, string> megas = new(System.StringComparer.Ordinal);  // mega id → kit id

        public StandIns(KitPlacer placer, WorldDto world)
        {
            this.placer = placer;
            var hosts = new HashSet<string>(world.Buildings.Select(m => FacadeKit.HostOf(m.Id)), System.StringComparer.Ordinal);
            var megaIds = new HashSet<string>(world.Megastructures.Select(m => m.Id), System.StringComparer.Ordinal);
            foreach (var sg in world.Signatures)
                if (sg.Kind == "tower" && sg.Kit != null && hosts.Contains(sg.Id)) Bind(towers, sg.Id, sg.Kit.Id, true);
            foreach (var (use, id) in placer.Uses)
            {
                if (!placer.Generated.ContainsKey(id)) continue; // adopted DecoKit pieces arrive through the export's kit refs
                if (use.StartsWith("landmark:") && megaIds.Contains("mega/" + use.Substring(9))) Bind(megas, "mega/" + use.Substring(9), id, true);
            }
        }

        static void Bind(Dictionary<string, string> map, string element, string id, bool present)
        {
            if (!present) { Debug.LogWarning($"World: kit `{id}` stands in for `{element}`, which this export lacks; not placed"); return; }
            if (map.TryGetValue(element, out var have)) throw new InvalidDataException($"World: `{element}` has two kit stand-ins, {have} and {id}");
            map[element] = id;
        }

        static bool TowerPart(MassDto m) => m.Id.EndsWith("/lower") || m.Id.EndsWith("/upper");

        /// <summary>A tower mass a piece replaces.</summary>
        public bool Covers(MassDto m) => TowerPart(m) && towers.ContainsKey(FacadeKit.HostOf(m.Id));
        /// <summary>A roof on a tower a piece replaces (the piece carries its own crown).</summary>
        public bool Covers(RoofDto r) => towers.ContainsKey(r.Building);
        public bool Covers(MegaDto m) => megas.ContainsKey(m.Id);

        public int Towers => towers.Count;
        public int Megas => megas.Count;

        /// <summary>The signature that covers a tower mass or roof (the kit id it is covered by).</summary>
        public string CoveredBy(string host) => towers.TryGetValue(host, out var id) ? id : null;

        /// <summary>Builds a covered mass: a marker over its own volume (the signatures layer stands the piece).</summary>
        public void Mass(Transform layer, MassDto m) => Combine.Cover(Nodes.Element(layer, m.Id), Prism(m.Ring, m.Base, m.Base + m.Height));

        public void Roof(Transform layer, RoofDto r)
        {
            var el = Nodes.Element(layer, r.Id);
            Combine.Cover(el, new Bounds(new Vector3(r.Centre.x, r.Z, r.Centre.y), new Vector3(r.Size.x, 0.1f, r.Size.y)));
        }

        /// <summary>The megastructure's piece under its element, standing on the lowest part base.</summary>
        public void Mega(Transform layer, Transform el, MegaDto m)
        {
            Bounds? b = null;
            foreach (var p in m.Parts) { var pb = Prism(p.Ring, p.Base, p.Base + p.Height); if (b is { } have) { have.Encapsulate(pb); b = have; } else b = pb; }
            var c = b ?? throw new InvalidDataException($"World: {m.Id} has no parts to stand its kit piece on");
            placer.Place(el, layer.name, megas[m.Id], new Vector2(c.center.x, c.center.z), c.min.y, 0f);
        }

        static Bounds Prism(Vector2[] ring, float z0, float z1)
        {
            Vector2 lo = ring[0], hi = ring[0];
            foreach (var q in ring) { lo = Vector2.Min(lo, q); hi = Vector2.Max(hi, q); }
            var b = new Bounds(); b.SetMinMax(new Vector3(lo.x, z0, lo.y), new Vector3(hi.x, z1, hi.y));
            return b;
        }
    }
}
