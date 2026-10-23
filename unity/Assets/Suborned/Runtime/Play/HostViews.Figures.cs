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
        sealed class Fig { public Transform model, tray; public Animator anim; public string id = "", state = ""; public bool female; public float yaw, yawVel; public float phase; public int sitTick = -1; public Dictionary<string, Transform> bones; public Vector3 lastPos; public float ground; /** brief 024 fire placeholder: shots seen, the last one's time, the draw's start, drawn last frame */ public int shots; public float shotAt = -10f, drawAt = -10f; public bool drawn; }
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
            an.runtimeAnimatorController = lib.ControllerFor(e); an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.CullCompletely;
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
                if (key == DriveKey)
                {
                    // Sky 2026-10-20 ("legs / feet all over the place"): the driven agent's legs follow the ground she really covers, never only the
                    // controller's wish: blocked by a planter she stood running on the spot. Smoothed over ~0.1 s so a walk never flickers to idle.
                    var mv = t.position - f.lastPos; mv.y = 0f; float dt = Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
                    float now = f.lastPos == Vector3.zero || mv.magnitude > 5f ? DriveSpeed : mv.magnitude / dt;
                    f.ground = Mathf.Lerp(f.ground, now, 1f - Mathf.Exp(-dt / 0.1f));
                    speed = Mathf.Min(DriveSpeed, f.ground);
                    f.yaw = Mathf.MoveTowardsAngle(f.yaw, DriveYaw, 540f * Time.unscaledDeltaTime); } // brief 025: the controller's speed and heading (Glyph: up to 540°/s)
                else if (speed > 0.15f) f.yaw = Mathf.SmoothDampAngle(f.yaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, ref f.yawVel, 0.1f, 720f, Time.unscaledDeltaTime); // Forma: turns eased (they snapped a corner a frame)
                else if (faces.TryGetValue(key, out var fc)) f.yaw = Mathf.MoveTowardsAngle(f.yaw, Mathf.Atan2(fc.x, fc.z) * Mathf.Rad2Deg, 360f * Time.unscaledDeltaTime); // turn to the table, or to him
                bool aiming = Aim.TryGetValue(key, out var aimAt);
                if (aiming) { var ad = aimAt - t.position; ad.y = 0f; if (ad.sqrMagnitude > 0.04f) f.yaw = Mathf.SmoothDampAngle(f.yaw, Mathf.Atan2(ad.x, ad.z) * Mathf.Rad2Deg, ref f.yawVel, 0.05f, 1800f, Time.unscaledDeltaTime); else aiming = false; } // Sky 2026-10-20: a shooter faces its target (~0.15 s), walking or not (it strafes)
                f.lastPos = t.position; // the driven agent's measured ground speed reads this next frame
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
                    if (cf != null && Bone(cf, HumanBodyBones.RightShoulder, "RightShoulder") is Transform rs) want = rs.position + right * 0.10f + Vector3.up * 0.10f;
                    f.model.rotation = Quaternion.Euler(0f, cyaw + 180f, 0f); f.model.position = feet;
                    var hips = Bone(f, HumanBodyBones.Hips, "Hips");
                    if (hips) f.model.position += want - hips.position; else f.model.position = want;
                }
                else if (carried)
                {
                    f.model.localPosition = new Vector3(0f, -lift, 0f); f.model.localRotation = Quaternion.Euler(0f, f.yaw + 90f, 0f);
                    var hips = Bone(f, HumanBodyBones.Hips, "Hips");
                    if (hips) { float want = f.model.position.y + 1.45f + 0.14f; f.model.position += Vector3.up * (want - hips.position.y); }
                }
                else
                {
                    f.model.localPosition = down ? new Vector3(0f, -lift, 0f) : Vector3.zero;
                    f.model.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
                }
                bool armed = Armed(key, g), startled = key.StartsWith("crowd/") && CrowdStartled(key, g) || BystanderStartled.Contains(key); // engine 0.18: the range's bystanders crouch too
                // brief 024 fire placeholder (Forma's clips aim_pistol, shoot, draw): raise to the aim while the agent aims or its fire state has a
                // target, a recoil restarted on every engine shot (the shot log's event, never a loop), the draw clip when the gun comes out, then lowered
                int opIdx = System.Array.IndexOf(OpKeys, key); float nowU = Time.unscaledTime;
                int shotN = shotEvents.TryGetValue(key, out var sn) ? sn : 0; bool shotNew = shotN != f.shots; f.shots = shotN; if (shotNew) f.shotAt = nowU;
                bool shotRecent = nowU - f.shotAt < ShootHoldS;
                if (opIdx >= 0 && armed && !f.drawn) f.drawAt = nowU; if (opIdx >= 0) f.drawn = armed;
                bool drawing = armed && nowU - f.drawAt < host.DrawS && host.DrawS > 0f;
                bool aimPose = opIdx >= 0 && armed && (key == DriveKey && host.Aiming || host.FireNow[opIdx].Mode.Length > 0 && host.FireNow[opIdx].Target.Length > 0);
                if (opIdx >= 0 && aimPose && key == DriveKey && host.Aiming && host.AimFaceOn && speed < 0.15f) f.yaw = Mathf.MoveTowardsAngle(f.yaw, host.AimFace, 900f * Time.unscaledDeltaTime); // the driven agent faces the reticle
                // Pulse's seats (HostSeats): a café customer standing on their chair within their span sits on it, facing the table (Forma's sit)
                var seat = !down && !carried && !startled && speed < 0.15f && Has(f, "sit") ? SeatNow(key, t.position, g) : null;
                if (seat is { } st) { f.yaw = st.yaw; f.model.SetPositionAndRotation(st.at, Quaternion.Euler(0f, st.yaw, 0f)); } else f.sitTick = -1;
                string state; float natural = 0f;
                if (carried) state = Has(f, "carried_drape") ? "carried_drape" : "carried"; // Forma: never the flat "carried" when the drape is there
                else if (carrying.Contains(key) && Has(f, "carry_shoulder")) state = speed > 0.15f ? "carry_shoulder" : "carry_shoulder_idle"; // nobody runs carrying
                else if (down) state = "lying";
                else if (startled && speed < 0.3f) state = "crouch";
                else if (speed > 2.0f) state = armed ? "run_armed" : key.StartsWith("op/") && Has(f, "run_lean") ? "run_lean" : "run"; // Forma: the cell's double-click run leans (run_lean, 2.72 m/s natural); the crowd and its panic keep run
                else if (speed > 0.15f) state = phases.TryGetValue(key, out var pw) && pw is "to table" or "from table" && Has(f, "serve_walk") ? "serve_walk" : f.female ? "walk_f" : "walk"; // Forma: walk_armed is a 2.44 m/s clip, slow motion at a walk; round 2: the tray carried to and from the table
                else if (phases.TryGetValue(key, out var ph) && ph == "serving") state = Has(f, "serve") ? "serve" : "talk"; // brief 022: at the table she serves (faces it), the tray on her hand
                else if (phases.TryGetValue(key, out var pt) && pt == "together") state = "talk"; // with the guard they talk
                else if (seat is { } so) state = SeatState(f, so.from, so.to, g); // HostSeats: sit down, a seated loop, stand up
                else if (opIdx >= 0 && shotRecent && Has(f, "shoot")) state = "shoot"; // brief 024 (Sky: "a clear animation when the player fires"): the recoil, restarted on every engine shot
                else if (opIdx >= 0 && drawing && Has(f, "draw")) state = "draw"; // the raise: the draw clip over the rulebook's draw time
                else if (aiming || aimPose) state = shotRecent && Has(f, "shoot") ? "shoot" : AimClip(f, key, g); // standing on a target: the aim, the shot when one was fired
                else state = armed && g.FightOn ? "aim_pistol" : f.phase > 0.5f ? "idle_alt" : "idle";
                natural = host.figures.Speed(f.id, state); if (natural <= 0f && state == "carry_shoulder") natural = 1.29f; // Forma: the walk's legs
                float stride = natural > 0f ? Mathf.Clamp(speed / natural, 0.4f, 1.8f) : 1f; // the floor was 0.6: a 0.52 m/s walker's legs walked 0.59 m/s and slid (crowd/33, brief 024 anim-fix)
                if (state == "draw") stride = DrawClipS / Mathf.Max(0.2f, host.DrawS); // the draw clip fits the rulebook's draw time
                f.anim.speed = (key == DriveKey ? 1f : pace) * stride; // the driven agent walks in an explore pause too
                Tray(f, state is "serve" or "serve_walk");
                if (state == "shoot" && shotNew && f.state == "shoot") f.anim.CrossFadeInFixedTime("shoot", 0.03f, 0, 0f); // the next shot: the recoil again from its start
                else if (state != f.state) { bool lower = f.state is "aim_pistol" or "aim" or "shoot" && state is "idle" or "idle_alt"; f.anim.CrossFadeInFixedTime(state, f.state.Length == 0 ? 0f : state == "shoot" ? 0.04f : lower ? 0.3f : f.state == "draw" ? 0.08f : 0.15f, 0, f.state.Length == 0 || (state is "sit" or "sit_alt" or "sit_talk" && f.state != "stand_to_sit") ? f.phase * 5f : 0f); f.state = state; } // Forma: seconds, not a share of the clip (an 8.8 s idle took 1.3 s to blend); lowering the gun eases over 0.3 s
                if (AnimLog != null && opIdx >= 0) LogFigure(key, f, state, shotNew, speed, opIdx);
            }
        }

        /// <summary>Forma round 2: the tray on the server's right hand (the library's prop, seated per figure), shown only while she serves or
        /// carries it to and from the table.</summary>
        void Tray(Fig f, bool on)
        {
            var lib = host.figures;
            if (on && f.tray == null && lib != null && lib.tray != null && lib.TraySeat(f.id) is int i && i >= 0 && Bone(f, HumanBodyBones.RightHand, "RightHand") is Transform hand)
            {
                var t = Object.Instantiate(lib.tray, hand, false).transform; t.name = "tray";
                t.localPosition = lib.trayPos[i]; t.localRotation = Quaternion.Euler(lib.trayEuler[i]); t.localScale = Vector3.one;
                f.tray = t;
            }
            if (f.tray != null && f.tray.gameObject.activeSelf != on) f.tray.gameObject.SetActive(on);
        }

        readonly HashSet<string> carrying = new();
        /// <summary>A bone by Humanoid role, or by name on a Generic rig (Forma's testbed cell: Mixamo names under Armature), cached per figure.</summary>
        static Transform Bone(Fig f, HumanBodyBones role, string name)
        {
            if (f.anim.isHuman) return f.anim.GetBoneTransform(role);
            f.bones ??= new Dictionary<string, Transform>();
            if (f.bones.TryGetValue(name, out var t) && t) return t;
            foreach (var c in f.model.GetComponentsInChildren<Transform>(true)) if (c.name == name || c.name.EndsWith(":" + name) || c.name == "mixamorig:" + name) { f.bones[name] = c; return c; }
            f.bones[name] = null; return null;
        }

        /// <summary>Brief 024: each engine shot by a view key (the shot log, HandoverHost.Shot): its figure plays the recoil once.</summary>
        readonly Dictionary<string, int> shotEvents = new();
        public void ShotEvent(string key) { if (string.IsNullOrEmpty(key)) return; shotEvents[key] = (shotEvents.TryGetValue(key, out var n) ? n : 0) + 1; }
        /// <summary>How long a shot holds the shoot clip (its 18 frames at 30 fps are 0.6 s; the next aim takes over after 0.45 s).</summary>
        const float ShootHoldS = 0.45f, DrawClipS = 2.0f;
        /// <summary>A figure's drawn yaw (Unity degrees), else null: the muzzle the HUD's tracer starts from.</summary>
        public float? FigureYaw(string key) => figs.TryGetValue(key, out var f) ? f.yaw : null;

        /// <summary>The per-frame animation log (player-animation-verification): on when not null; one TSV row per cell figure per frame.</summary>
        public List<string> AnimLog;
        public const string AnimLogHeader = "frame	t	key	state	clip	norm	transition	speed	ground	isHuman	shotEvent	shots	fireMode	firing	shot	aiming";
        void LogFigure(string key, Fig f, string state, bool shotNew, float ground, int k)
        {
            var info = f.anim.GetCurrentAnimatorStateInfo(0); var clips = f.anim.GetCurrentAnimatorClipInfo(0); string clip = clips.Length > 0 && clips[0].clip ? clips[0].clip.name : "";
            var fs = host.FireNow[k];
            AnimLog.Add($"{Time.frameCount}	{Time.unscaledTime:0.000}	{key}	{state}	{clip}	{info.normalizedTime:0.000}	{(f.anim.IsInTransition(0) ? 1 : 0)}	{f.anim.speed:0.00}	{ground:0.00}	{(f.anim.isHuman ? 1 : 0)}	{(shotNew ? 1 : 0)}	{f.shots}	{fs.Mode}	{(fs.Firing ? 1 : 0)}	{(fs.Shot ? 1 : 0)}	{(key == DriveKey && host.Aiming ? 1 : 0)}");
            if (AnimLog.Count > 40000) AnimLog.RemoveRange(0, 10000);
        }

        /// <summary>The aim clip for a shooter: a long gun's "aim" (Forma: tb_aim, tb_fire_rifle), else the pistol's "aim_pistol".</summary>
        string AimClip(Fig f, string key, Game g)
        {
            string w = ""; try { var id = key.StartsWith("op/") ? key[3..] : null; var gun = id != null ? g.GunOf(id) : null; w = gun?.W?.Id ?? ""; } catch { }
            bool rifle = w.Contains("rifle") || w.Contains("riot") || w.Contains("long");
            return rifle && Has(f, "aim") ? "aim" : Has(f, "aim_pistol") ? "aim_pistol" : Has(f, "aim") ? "aim" : "idle";
        }

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
            // brief 026: a robot model gets its live face and servo head (robots.json), seeded by the host key; view only
            if (host.robotRigs != null && host.robotGlyphs && Suborned.Robots.RobotRigs.Attach(inst, host.robotRigs, host.robotGlyphs, key, Suborned.Robots.RobotRigs.KindOf(model.name)) != null
                && inst.GetComponent<Suborned.Robots.RobotIdle>() is { } idle) robotHeads[key] = (idle, false);
            else if (Suborned.Robots.RobotRigs.KindOf(model.name) == null) StageRobot(key, inst); // brief 031: the patrol robot's stage face (M_RobotFace), when the model has one
            return inst.transform;
        }
        readonly Dictionary<string, Transform> carModels = new();
        readonly Dictionary<string, (Suborned.Robots.RobotIdle idle, bool alarmed)> robotHeads = new();
        static readonly List<string> robotKeys = new();

        /// <summary>Brief 026: the robots' faces follow the response, for the eye only (nothing goes back to the engine). A response robot on
        /// the street is answering an alarm: red, angry, its head on the nearest operative within 25 m. The patrol robot turns so at Alert 2
        /// (search) and calms below it. Faces and glances run on Time.time (a paused game freezes them).</summary>
        void RobotFaces(Game g)
        {
            if (robotHeads.Count == 0) return;
            robotKeys.Clear(); robotKeys.AddRange(robotHeads.Keys);
            foreach (var key in robotKeys)
            {
                var (idle, was) = robotHeads[key]; if (!idle) continue;
                bool shown = things.TryGetValue(key, out var th) && th.shown;
                bool alarmed = shown && (key == "patrol" ? g.Alert >= 2 && !g.PatrolDown : key.StartsWith("unit/"));
                Transform suspect = null; float best = 25f * 25f;
                if (alarmed) for (int i = 0; i < 4; i++) if (things.TryGetValue(OpKeys[i], out var op) && op.shown) { float d = (op.t.position - idle.transform.position).sqrMagnitude; if (d < best) { best = d; suspect = op.t; } }
                if (alarmed && !was) idle.Alarm(suspect, Time.time);
                else if (!alarmed && was) idle.Calm(Time.time);
                else if (alarmed) idle.Track(suspect);
                if (alarmed != was) robotHeads[key] = (idle, alarmed);
            }
        }
        /// <summary>The cars face where they drive (the official car comes to the pickup and leaves); standing, they keep their last heading.
        /// The official car points by the engine's own heading where it gives one (Game.CarHeading, brief 023: along its drive while it moves,
        /// the site's car.serviceFacing once parked in the service bay): nose-first down the service drive and nose-in in the bay.</summary>
        void Cars()
        {
            foreach (var kv in carModels)
            {
                var (t, from, to, shown) = things[kv.Key]; if (!shown) continue;
                if (Aim.TryGetValue(kv.Key, out var aimAt)) { var ad = aimAt - t.position; ad.y = 0f; if (ad.sqrMagnitude > 0.04f) { kv.Value.rotation = Quaternion.RotateTowards(kv.Value.rotation, Quaternion.LookRotation(ad.normalized, Vector3.up) * carNose, 900f * Time.unscaledDeltaTime); continue; } } // a robot or vehicle that shoots faces its target (the shared rule)
                if (kv.Key == "car/official" && CarFacing() is Vector3 h) { kv.Value.localRotation = Quaternion.LookRotation(h, Vector3.up) * carNose; continue; }
                if (kv.Key == "patrol" && host.Game?.PatrolPose() is { } pp && (pp.facing.X != 0 || pp.facing.Y != 0)) // brief 031: a robot looks the way its face points: the patrol faces its sweep's facing (the cone), not its walk
                { var pf = frame.Dir(pp.facing); kv.Value.rotation = Quaternion.RotateTowards(kv.Value.rotation, Quaternion.LookRotation(pf, Vector3.up), 360f * Time.unscaledDeltaTime); continue; }
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
                if (!things.TryGetValue(key, out var th) || !th.shown || !th.t.gameObject.activeInHierarchy || lifted.ContainsKey(key) || host.TargetRingKeys.Contains(key)) return; // a downed rival lies without one; a target ring replaces it (brief 049)
                if (rivalRings.Count <= used)
                {
                    var go = new GameObject("rivalring/" + used); go.transform.SetParent(root, false); var lr = go.AddComponent<LineRenderer>();
                    var c = host.rivalColour; rivalRingMat ??= host.SignalMat(c, false); // a signal: no fog
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
