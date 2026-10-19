using System.Collections.Generic;
using Suborned.Sim;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 020 (Sky: "using some of our existing player models for now"): the stand-in figures (Forma's FigureLibrary) inside the
    /// people's views. A view keeps its capsule collider, its anchors and its key; only the capsule's renderers go dark. The figure follows the
    /// view's own eased position and the engine's state, never the other way: it faces where it moves, walks or runs with the stride matched to
    /// the ground speed (the clips' natural speeds from Forma's manifest, tools/figures/speeds.json: walk 1.29, walk_f 0.99, run 2.52 m/s), lies where a body lies, rides the carrier's shoulder when carried,
    /// and draws its weapon in a fight. View only: nothing here is read by the engine. Keys with no figure keep their capsule.</summary>
    public sealed partial class HostViews
    {
        sealed class Fig { public Transform model, tray; public Animator anim; public string id = "", state = ""; public bool female; public float yaw; public float phase; public int sitTick = -1; }
        readonly Dictionary<string, Fig> figs = new(); readonly Dictionary<string, Vector3> faces = new(); // brief 022: a pose's facing, while standing
        readonly Dictionary<string, string> phases = new(); // brief 022: a server's round phase (at post, to table, serving, from table, held, to post, together)
        readonly Dictionary<string, float> lifted = new(); // a view's lift (m) while down: 0 on the ground, 1.15 on a shoulder
        readonly Dictionary<string, string> carriers = new(); // a carried body's view key → its carrier's view key

        /// <summary>The figure for a fixed view key (the cell, the cast, the patrol, the legacy officers, the rivals), else null.</summary>
        static string RoleFigure(string key) => key switch
        {
            "op/scout" => "scout", "op/fixer" => "fixer", "op/tech" => "tech", "op/enforcer" => "enforcer",
            "waitress" => "waitress", "guard" => "guard", "driver" => "driver", "councillor" => "councillor",
            "patrol" => null, _ when key.StartsWith("security/") || key.StartsWith("unit/") => "security", _ when key.StartsWith("rivals/") => "rival",
            _ => null,
        };

        /// <summary>A person's figure: the cast by role, anyone else from the pool for their job (the same person always the same figure).</summary>
        string PersonFigure(Person p, string salt = "")
        {
            var lib = host.figures; if (lib == null || p == null) return null;
            if (p.Role.Length > 0 && RoleFigure(p.Role) is string r) return r;
            var job = p.Job ?? "";
            string fam = job is "server" or "cook" or "waitress" ? "cafe" : job is "clerk" or "attendant" or "councillor" ? "office" : job == "guard" ? "guard" : job is "driver" or "official-driver" ? "driver" : "passer";
            return lib.Pick("job/" + job, p.Id + salt) ?? lib.Pick("family/" + fam, p.Id + salt);
        }

        /// <summary>Glyph 022 (two identical blond passers-by walking side by side): walkers who set out from the same place at the same time
        /// walk together, so none of them shares a figure while the pool has another (the pick is re-seeded, still by the person's id).</summary>
        void DressCrowd(Game g)
        {
            var used = new Dictionary<string, HashSet<string>>();
            for (int i = 0; i < g.Walkers.Count; i++)
            {
                var w = g.Walkers[i]; string grp = w.Place.Length > 0 && w.From.Length > 0 ? w.Place[0] + "@" + w.From[0] : "";
                if (!used.TryGetValue(grp, out var set)) used[grp] = set = new HashSet<string>();
                var fid = PersonFigure(w.P);
                for (int t = 1; t < 8 && fid != null && grp.Length > 0 && set.Contains(fid); t++) fid = PersonFigure(w.P, "#" + t);
                if (fid != null) set.Add(fid);
                Dress(crowdKeys[i], fid);
            }
        }

        /// <summary>Put figure `id` in the view `key` (or swap it); null keeps the capsule.</summary>
        void Dress(string key, string id)
        {
            var lib = host.figures; if (lib == null || id == null || !things.TryGetValue(key, out var th)) return;
            if (figs.TryGetValue(key, out var have)) { if (have.id == id) return; Object.Destroy(have.model.gameObject); figs.Remove(key); }
            var e = lib.Find(id); if (e?.model == null || lib.controller == null) return;
            var t = th.t; var inst = Object.Instantiate(e.model, t, false); inst.name = "figure";
            if (inst.GetComponentInChildren<LODGroup>() is LODGroup lg && e.lodHeights.Length > 0)
            {
                var lods = lg.GetLODs(); for (int i = 0; i < lods.Length; i++) lods[i].screenRelativeTransitionHeight = e.lodHeights[Mathf.Min(i, e.lodHeights.Length - 1)]; lg.SetLODs(lods);
            }
            var an = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            an.runtimeAnimatorController = lib.controller; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.CullCompletely;
            foreach (Transform c in t) if (c != inst.transform && c.GetComponent<MeshRenderer>() is MeshRenderer mr) mr.enabled = false; // the capsule's own parts go dark
            uint h = 2166136261; foreach (var ch in key) { h ^= ch; h *= 16777619; }
            figs[key] = new Fig { model = inst.transform, anim = an, id = id, female = e.female, phase = (h % 1000) / 1000f };
        }

        /// <summary>Per frame, after the positions: each figure's facing, clip and stride.</summary>
        void Figures(float alpha)
        {
            if (figs.Count == 0) return;
            var g = host.Game; float pace = host.Paused || g.Over ? 0f : host.Speed;
            carrying.Clear(); if (g.Vale == ValeAt.WithEnforcer) carrying.Add("op/enforcer"); // who has someone on the shoulder this frame
            foreach (var kv in carriers) if (lifted.TryGetValue(kv.Key, out var lc) && lc > 0.5f && things.TryGetValue(kv.Key, out var bt) && bt.shown) carrying.Add(kv.Value);
            for (int pass = 0; pass < 2; pass++) // the carried after everyone else: they sit on their carrier's shoulders as he stands this frame
            foreach (var kv in figs)
            {
                var key = kv.Key; var f = kv.Value; var (t, from, to, shown) = things[key];
                if (!shown || !t.gameObject.activeInHierarchy) continue;
                bool late = (key == "councillor" && g.Vale == ValeAt.WithEnforcer) || (lifted.TryGetValue(key, out var lf) && lf > 0.5f);
                if (late != (pass == 1)) continue;
                if (t.localScale != Vector3.one) t.localScale = Vector3.one;            // the capsule's cower squash: the figure crouches instead
                bool down = lifted.TryGetValue(key, out var lift); if (down && t.localRotation != Quaternion.identity) t.localRotation = Quaternion.identity; // laid flat is the capsule's: the figure lies
                var d = to - from; d.y = 0f; float speed = d.magnitude / 0.1f;          // the engine's step is a tenth of a second
                if (key == DriveKey) { speed = DriveSpeed; f.yaw = Mathf.MoveTowardsAngle(f.yaw, DriveYaw, 540f * Time.unscaledDeltaTime); } // brief 025: the controller's speed and heading (Glyph: up to 540°/s)
                else if (speed > 0.15f) f.yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                else if (faces.TryGetValue(key, out var fc)) f.yaw = Mathf.MoveTowardsAngle(f.yaw, Mathf.Atan2(fc.x, fc.z) * Mathf.Rad2Deg, 360f * Time.unscaledDeltaTime); // turn to the table, or to him
                bool vale = key == "councillor" && g.Vale == ValeAt.WithEnforcer, carried = (down && lift > 0.5f) || vale;
                string carrierKey = vale ? "op/enforcer" : carried && carriers.TryGetValue(key, out var ck) ? ck : null;
                if (carried && carrierKey != null && things.TryGetValue(carrierKey, out var cv) && cv.shown)
                {
                    // Forma 2026-10-18 (forLatch 7, the fireman's carry; Glyph: Vale read as a dark bar across the shoulders): the carried figure
                    // turned to the carrier's heading + 180 (trunk down his back, legs down his chest), its hips on his right shoulder bone,
                    // 0.10 m to his right and 0.10 m up; recomputed each frame from the bone. Without a carrier figure: 1.45 m over his feet.
                    float cyaw = figs.TryGetValue(carrierKey, out var cf) ? cf.yaw : f.yaw;
                    var feet = cf != null ? cf.model.position : cv.t.position; var right = Quaternion.Euler(0f, cyaw, 0f) * Vector3.right;
                    var want = feet + Vector3.up * 1.55f + right * 0.15f;
                    if (cf != null && cf.anim.GetBoneTransform(HumanBodyBones.RightShoulder) is Transform rs) want = rs.position + right * 0.10f + Vector3.up * 0.10f;
                    f.model.rotation = Quaternion.Euler(0f, cyaw + 180f, 0f); f.model.position = feet;
                    var hips = f.anim.GetBoneTransform(HumanBodyBones.Hips);
                    if (hips) f.model.position += want - hips.position; else f.model.position = want;
                }
                else if (carried)
                {
                    f.model.localPosition = new Vector3(0f, -lift, 0f); f.model.localRotation = Quaternion.Euler(0f, f.yaw + 90f, 0f);
                    var hips = f.anim.GetBoneTransform(HumanBodyBones.Hips);
                    if (hips) { float want = f.model.position.y + 1.45f + 0.14f; f.model.position += Vector3.up * (want - hips.position.y); }
                }
                else
                {
                    f.model.localPosition = down ? new Vector3(0f, -lift, 0f) : Vector3.zero;
                    f.model.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
                }
                bool armed = Armed(key, g), startled = key.StartsWith("crowd/") && CrowdStartled(key, g);
                // Pulse's seats (HostSeats): a café customer standing on their chair within their span sits on it, facing the table (Forma's sit)
                var seat = !down && !carried && !startled && speed < 0.15f && Has(f, "sit") ? SeatNow(key, t.position, g) : null;
                if (seat is { } st) { f.yaw = st.yaw; f.model.SetPositionAndRotation(st.at, Quaternion.Euler(0f, st.yaw, 0f)); } else f.sitTick = -1;
                string state; float natural = 0f;
                if (carried) state = Has(f, "carried_drape") ? "carried_drape" : "carried"; // Forma: never the flat "carried" when the drape is there
                else if (carrying.Contains(key) && Has(f, "carry_shoulder")) state = speed > 0.15f ? "carry_shoulder" : "carry_shoulder_idle"; // nobody runs carrying
                else if (down) state = "lying";
                else if (startled && speed < 0.3f) state = "crouch";
                else if (speed > 2.0f) state = armed ? "run_armed" : "run";
                else if (speed > 0.15f) state = phases.TryGetValue(key, out var pw) && pw is "to table" or "from table" && Has(f, "serve_walk") ? "serve_walk" : f.female ? "walk_f" : "walk"; // Forma: walk_armed is a 2.44 m/s clip, slow motion at a walk; round 2: the tray carried to and from the table
                else if (phases.TryGetValue(key, out var ph) && ph == "serving") state = Has(f, "serve") ? "serve" : "talk"; // brief 022: at the table she serves (faces it), the tray on her hand
                else if (phases.TryGetValue(key, out var pt) && pt == "together") state = "talk"; // with the guard they talk
                else if (seat is { } so) state = SeatState(f, so.from, so.to, g); // HostSeats: sit down, a seated loop, stand up
                else state = armed && g.FightOn ? "aim_pistol" : f.phase > 0.5f ? "idle_alt" : "idle";
                natural = host.figures.Speed(state); if (natural <= 0f && state == "carry_shoulder") natural = 1.29f; // Forma: the walk's legs
                float stride = natural > 0f ? Mathf.Clamp(speed / natural, 0.6f, 1.8f) : 1f;
                f.anim.speed = (key == DriveKey ? 1f : pace) * stride; // the driven agent walks in an explore pause too
                Tray(f, state is "serve" or "serve_walk");
                if (state != f.state) { f.anim.CrossFadeInFixedTime(state, f.state.Length == 0 ? 0f : 0.15f, 0, f.state.Length == 0 || (state is "sit" or "sit_alt" or "sit_talk" && f.state != "stand_to_sit") ? f.phase * 5f : 0f); f.state = state; } // Forma: seconds, not a share of the clip (an 8.8 s idle took 1.3 s to blend)
            }
        }

        /// <summary>Forma round 2: the tray on the server's right hand (the library's prop, seated per figure), shown only while she serves or
        /// carries it to and from the table.</summary>
        void Tray(Fig f, bool on)
        {
            var lib = host.figures;
            if (on && f.tray == null && lib != null && lib.tray != null && lib.TraySeat(f.id) is int i && i >= 0 && f.anim.GetBoneTransform(HumanBodyBones.RightHand) is Transform hand)
            {
                var t = Object.Instantiate(lib.tray, hand, false).transform; t.name = "tray";
                t.localPosition = lib.trayPos[i]; t.localRotation = Quaternion.Euler(lib.trayEuler[i]); t.localScale = Vector3.one;
                f.tray = t;
            }
            if (f.tray != null && f.tray.gameObject.activeSelf != on) f.tray.gameObject.SetActive(on);
        }

        readonly HashSet<string> carrying = new();
        static bool Has(Fig f, string state) => f.anim.runtimeAnimatorController != null && f.anim.HasState(0, Animator.StringToHash(state));

        /// <summary>Brief 020: a kit model as the visual of a box or drum view (the view's collider stays the click target); the view's own
        /// "body" renderer goes dark. Returns the model, or null when the kit is not there.</summary>
        Transform Visual(string key, GameObject model)
        {
            if (model == null || !things.TryGetValue(key, out var th)) return null;
            var t = th.t; var inst = Object.Instantiate(model, t, false); inst.name = "model";
            // until it moves, a car stands along its street, never nose to the kerb: the getaway car along its exit lane; the official car
            // across the line from it to the driver's meeting point (the pavement beside it)
            var S = host.Game.S; Vector3 along = Vector3.zero;
            if (key == "car/getaway" && S.ExitLane.Length >= 2) along = frame.World(S.ExitLane[S.ExitLane.Length - 1]) - frame.World(S.ExitLane[0]);
            else if (key == "car/official" && CarFacing() is Vector3 cf) along = cf; // the engine's heading where it gives one (brief 023)
            else if (key == "car/official" && S.HasPlace("d:Meet driver")) { var side = frame.World(S.At("d:Meet driver")) - frame.World(host.Game.CarPos()); along = new Vector3(-side.z, 0f, side.x); }
            along.y = 0f; if (along.sqrMagnitude > 0.01f) inst.transform.localRotation = Quaternion.LookRotation(along.normalized, Vector3.up);
            if (t.Find("body")?.GetComponent<MeshRenderer>() is MeshRenderer mr) mr.enabled = false;
            foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return inst.transform;
        }
        readonly Dictionary<string, Transform> carModels = new();
        /// <summary>The cars face where they drive (the official car comes to the pickup and leaves); standing, they keep their last heading.
        /// The official car points by the engine's own heading where it gives one (Game.CarHeading, brief 023: along its drive while it moves,
        /// the site's car.serviceFacing once parked in the service bay): nose-first down the service drive and nose-in in the bay.</summary>
        void Cars()
        {
            foreach (var kv in carModels)
            {
                var (t, from, to, shown) = things[kv.Key]; if (!shown) continue;
                if (kv.Key == "car/official" && CarFacing() is Vector3 h) { kv.Value.localRotation = Quaternion.LookRotation(h, Vector3.up) * carNose; continue; }
                var d = to - from; d.y = 0f; if (d.sqrMagnitude > 0.0025f) kv.Value.localRotation = Quaternion.LookRotation(d.normalized, Vector3.up) * carNose;
            }
        }

        /// <summary>The engine's heading for the official car, as a world direction, else null (the host's own rule: its motion, or along its street).</summary>
        Vector3? CarFacing()
        {
            var g = host.Game; if (g == null) return null;
            Pt? h = null; try { h = g.CarHeading(); } catch { return null; }
            if (h is not Pt v || (v.X == 0 && v.Y == 0)) return null;
            var at = g.CarPos(); var w = frame.World(new Pt(at.X + v.X, at.Y + v.Y)) - frame.World(at); w.y = 0f;
            return w.sqrMagnitude > 1e-8f ? w.normalized : null;
        }
        /// <summary>The sedans' nose (Vela: -Y in Blender) as the model is imported; a moving car turns so its nose leads.</summary>
        static readonly Quaternion carNose = Quaternion.identity;

        readonly LineRenderer[] cellRings = new LineRenderer[4];
        /// <summary>Glyph (people r1): every cell member a thin cyan ring at the feet, thicker and brighter when selected, so a black-and-white
        /// passer-by can't pass for one of the cell; with figures it replaces the cream selection disc.</summary>
        void CellRings()
        {
            if (host.figures == null) return;
            for (int k = 0; k < 4; k++)
            {
                var (t, _, _, shown) = things[OpKeys[k]]; bool on = shown && figs.ContainsKey(OpKeys[k]) && t.gameObject.activeInHierarchy;
                if (cellRings[k] == null)
                {
                    var go = new GameObject("cellring/" + k); go.transform.SetParent(root, false); var lr = go.AddComponent<LineRenderer>();
                    lr.sharedMaterial = Unlit(new Color(0.376f, 0.878f, 0.918f)); lr.loop = true; lr.positionCount = 32; lr.useWorldSpace = false; lr.alignment = LineAlignment.TransformZ;
                    go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    for (int i = 0; i < 32; i++) { float a = i * Mathf.PI * 2f / 32f; lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.42f, Mathf.Sin(a) * 0.42f, 0f)); }
                    cellRings[k] = lr;
                }
                var r = cellRings[k]; if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on); if (!on) continue;
                bool sel = host.Selected[k]; r.transform.position = t.position + Vector3.up * 0.04f; r.widthMultiplier = sel ? 0.11f : 0.045f;
                var c = new Color(0.376f, 0.878f, 0.918f, sel ? 1f : 0.7f); r.startColor = r.endColor = c;
                var disc = things[SelKeys[k]].t; if (disc.gameObject.activeSelf) disc.gameObject.SetActive(false); // the ring is the selection
            }
        }

        readonly List<LineRenderer> rivalRings = new(); Material rivalRingMat;
        /// <summary>Brief 020 (Glyph): the rivals read only by their violet eye over navy clothes; a thin violet ring at their feet, as the cell's
        /// cyan rings, says who they are from above: the crew walking to Vale (rivals/0-2) and, in the fight, every rival hostile standing.</summary>
        void RivalRings()
        {
            var g = host.Game; int used = 0;
            void Ring(string key)
            {
                if (!things.TryGetValue(key, out var th) || !th.shown || !th.t.gameObject.activeInHierarchy || lifted.ContainsKey(key)) return; // a downed rival lies without one
                if (rivalRings.Count <= used)
                {
                    var go = new GameObject("rivalring/" + used); go.transform.SetParent(root, false); var lr = go.AddComponent<LineRenderer>();
                    var c = host.rivalColour; rivalRingMat ??= Unlit(c);
                    lr.sharedMaterial = rivalRingMat; lr.loop = true; lr.positionCount = 32; lr.useWorldSpace = false; lr.alignment = LineAlignment.TransformZ;
                    go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    for (int i = 0; i < 32; i++) { float a = i * Mathf.PI * 2f / 32f; lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.42f, Mathf.Sin(a) * 0.42f, 0f)); }
                    lr.widthMultiplier = 0.055f; lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, 0.9f); // Glyph 022: 2 px at 1080p play zoom (38.6 px/m)
                    rivalRings.Add(lr);
                }
                var r = rivalRings[used++]; if (!r.gameObject.activeSelf) r.gameObject.SetActive(true);
                r.transform.position = th.t.position + Vector3.up * 0.04f;
            }
            for (int k = 0; k < 3; k++) Ring("rivals/" + k);
            if (g != null) for (int i = 0; i < hostileKeys.Length && i < g.Hostiles.Count; i++) if (g.Hostiles[i].Id.StartsWith("rival")) Ring(hostileKeys[i]);
            for (int i = used; i < rivalRings.Count; i++) if (rivalRings[i].gameObject.activeSelf) rivalRings[i].gameObject.SetActive(false);
        }

        bool Armed(string key, Game g)
        {
            for (int k = 0; k < 4; k++) if (key == OpKeys[k]) { var gun = g.Ops[k].Gun; return gun.W != null && gun.Drawn; }
            return g.FightOn && (key.StartsWith("hostile/") || key.StartsWith("rivals/") || key.StartsWith("security/") || key.StartsWith("unit/") || key == "patrol");
        }
        bool CrowdStartled(string key, Game g) { int i = int.Parse(key.Substring(6)); return i < g.Walkers.Count && g.Walkers[i].St == "startled"; }
    }
}
