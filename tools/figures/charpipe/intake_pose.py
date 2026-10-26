"""Intake stage, step 2 (brief 069 v2): the MPFB base posed into the intake A-pose (intake.json 'pose'), man and woman at the reference macros, rendered as
the grey mannequin reference sheet in the intake layout (intake.json 'sheet'). The same picture is (a) the pose reference the image model gets with the
prompt and (b) the base the pose check measures a still against; the posed base is saved as the wrap target.
  blender -b --factory-startup -P tools/figures/charpipe/intake_pose.py -- <out dir>
Writes <out>/mannequin_<sex>.png (1536x1024), <out>/wrap_target_<sex>.blend, <out>/intake_pose_<sex>.json (the posed base re-measured)."""
import bpy, addon_utils, json, math, os, sys
from mathutils import Vector, Matrix

A = sys.argv[sys.argv.index('--') + 1:]; OUT = A[0]; os.makedirs(OUT, exist_ok=True)
WRONG = '--wrong' in A      # a deliberately wrong pose (feet out 20 deg, arms at 30 deg, legs apart): the pose check's negative test
HERE = os.path.dirname(os.path.abspath(__file__)); IN = json.load(open(os.path.join(HERE, 'intake.json'))); POSE = IN['pose']; FILL = IN['sheet']['figureHeightFraction']
MPFB = os.environ['MPFBDATA']; P = 'mixamorig:'; PROXY = {'m': 'male1591', 'f': 'female1605'}


def aim(pb, want):
    head = pb.head.copy(); cur = (pb.tail - pb.head).normalized(); q = cur.rotation_difference(want.normalized())
    pb.matrix = Matrix.Translation(head) @ q.to_matrix().to_4x4() @ Matrix.Translation(-head) @ pb.matrix; bpy.context.view_layer.update()


def twist(pb, axis, ang):
    head = pb.head.copy(); pb.matrix = Matrix.Translation(head) @ Matrix.Rotation(ang, 4, axis) @ Matrix.Translation(-head) @ pb.matrix; bpy.context.view_layer.update()


def palm_normal(arm, s):
    Pb = arm.pose.bones; W = arm.matrix_world
    w, i1, p1 = W @ Pb[P + s + 'Hand'].head, W @ Pb[P + s + 'HandIndex1'].head, W @ Pb[P + s + 'HandPinky1'].head
    return ((i1 - w).cross(p1 - w)).normalized() * (1 if s == 'Left' else -1)


def pose_base(arm):
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE'); Pb = arm.pose.bones
    a = math.radians(30 if WRONG else POSE['armDownFromHorizontalDeg']); fw = math.radians(POSE['armForwardDeg'])
    for s, sx in (('Left', 1.0), ('Right', -1.0)):
        d = Vector((sx * math.cos(a) * math.cos(fw), -math.cos(a) * math.sin(fw), -math.sin(a))).normalized()
        for b in ('Arm', 'ForeArm', 'Hand'): aim(Pb[P + s + b], d)                           # elbow and wrist straight
        # palms toward the body: twist the forearm about the arm until the palm's normal points at the body (in, and down along the A)
        # palms toward the body = thumbs forward: twist the forearm about the arm until the thumb's first joint points forward (-Y), the smaller way round
        th = Pb[P + s + 'HandThumb1']; t = (th.tail - th.head); t = (t - d * t.dot(d)).normalized()
        want = Vector((0, -1, 0)); want = (want - d * want.dot(d)).normalized()
        ang = math.atan2(d.dot(t.cross(want)), t.dot(want)); twist(Pb[P + s + 'ForeArm'], d, ang)
        for f in ('Index', 'Middle', 'Ring', 'Pinky', 'Thumb'):                              # fingers straight along their first joint
            f1 = Pb[P + s + 'Hand' + f + '1']; df = (f1.tail - f1.head).normalized()
            if f != 'Thumb': df = (df - d * 0.0).normalized()
            for i in ('2', '3'): aim(Pb[P + s + 'Hand' + f + i], df)
        for b in ('UpLeg', 'Leg'): aim(Pb[P + s + b], Vector((sx * 0.12, 0, -1)) if WRONG else Vector((0, 0, -1)))                    # legs straight down: each ankle under its hip joint
        for b in ('Foot', 'ToeBase'):                                                        # feet straight ahead, their own pitch kept
            pb = Pb[P + s + b]; v = pb.tail - pb.head; pitch = math.atan2(-v.z, math.hypot(v.x, v.y)) if b == 'Foot' else 0.0
            yw = math.radians(20 if WRONG else POSE['footYawDeg']) * sx
            aim(pb, Vector((math.sin(yw) * math.cos(pitch), -math.cos(yw) * math.cos(pitch), -math.sin(pitch))))
    bpy.ops.object.mode_set(mode='OBJECT')


def measure(arm, body):
    W = arm.matrix_world; Pb = arm.pose.bones; H = lambda n: W @ Pb[P + n].head
    zs = [(body.matrix_world @ v.co).z for v in body.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]; h = max(zs) - min(zs)
    a = H('LeftForeArm') - H('LeftArm'); ft = H('LeftToeBase') - H('LeftFoot')
    return {'standingHeightM': round(h, 3), 'armDownFromHorizontal': round(math.degrees(math.atan2(-a.z, math.hypot(a.x, a.y))), 1),
            'elbowBend': round(math.degrees((H('LeftForeArm') - H('LeftArm')).angle(H('LeftHand') - H('LeftForeArm'))), 1),
            'palmNormalLeft': [round(x, 2) for x in palm_normal(arm, 'Left')],
            'hipJointWidthFraction': round((H('LeftUpLeg') - H('RightUpLeg')).length / h, 3), 'ankleSpacingFraction': round(abs((H('LeftFoot') - H('RightFoot')).x) / h, 3),
            'footYawOut': round(math.degrees(math.atan2(ft.x, -ft.y)), 1), 'wristHeightFraction': round(H('LeftHand').z / h, 3),
            'handOutFraction': round(H('LeftHand').x / h, 3)}


def render(sex, body):
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'SINGLE'; sc.display.shading.single_color = (0.78, 0.78, 0.78)
    sc.display.shading.show_cavity = False; sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('w'); sc.world = w; w.color = (0.188, 0.188, 0.188)          # sRGB 120 (linear 0.188)
    sc.render.resolution_x = 512; sc.render.resolution_y = 1024; sc.render.film_transparent = False
    zs = [(body.matrix_world @ v.co).z for v in body.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]; h = max(zs) - min(zs)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.sensor_fit = 'VERTICAL'; cam.data.ortho_scale = h / FILL
    tiles = []
    for kind in ('m', 's'):          # m: the shaded mannequin (the image model's reference); s: a flat silhouette (the pose check's base)
        sc.display.shading.light = 'STUDIO' if kind == 'm' else 'FLAT'
        sc.display.shading.single_color = (0.78, 0.78, 0.78) if kind == 'm' else (1.0, 1.0, 1.0)
        for name, loc, rot in (('front', (0, -10, h / 2), (90, 0, 0)), ('left', (10, 0, h / 2), (90, 0, 90)), ('back', (0, 10, h / 2), (90, 0, 180))):
            cam.location = loc; cam.rotation_euler = tuple(math.radians(r) for r in rot)
            p = os.path.join(OUT, f'_{kind}_{sex}_{name}{"_wrong" if WRONG else ""}.png'); sc.render.filepath = p; bpy.ops.render.render(write_still=True); tiles.append(p)
    return tiles


res = {}
for sex in (('m',) if WRONG else ('m', 'f')):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    addon_utils.enable('bl_ext.user_default.mpfb', default_set=True, persistent=False)
    from bl_ext.user_default.mpfb.services.humanservice import HumanService
    from bl_ext.user_default.mpfb.services.targetservice import TargetService
    m = TargetService.get_default_macro_info_dict(); m.update({'gender': 1.0 if sex == 'm' else 0.0})
    b = HumanService.create_human(scale=0.1, macro_detail_dict=m); arm = HumanService.add_builtin_rig(b, 'mixamo_unity')
    body = HumanService.add_mhclo_asset(f'{MPFB}/proxymeshes/{PROXY[sex]}/{PROXY[sex]}.proxy', b, asset_type='Proxymeshes', subdiv_levels=0)
    bpy.data.objects.remove(b, do_unlink=True)
    pose_base(arm)
    res[sex] = measure(arm, body); res[sex]['tiles'] = render(sex, body)
    if not WRONG: bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, f'wrap_target_{sex}.blend'), compress=False)
if not WRONG: json.dump(res, open(os.path.join(OUT, 'intake_pose.json'), 'w'), indent=1)
print('POSED', json.dumps(res))
