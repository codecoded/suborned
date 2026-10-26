"""Intake stage, step 1 (brief 069 v2): measure the MPFB base's rest pose (MPFB's own A-pose, before any T-pose), man and woman at the reference macros,
so the intake prompt can ask for exactly that pose. Blender side.
  blender -b --factory-startup -P tools/figures/charpipe/intake_measure.py -- <out base_pose.json>
Angles in degrees, lengths in metres and as a fraction of standing height. Front = -Y, up = +Z, the figure's left = +X."""
import bpy, addon_utils, json, math, os, sys
from mathutils import Vector

MPFB = os.environ['MPFBDATA']; P = 'mixamorig:'


def deg(x): return round(math.degrees(x), 1)


def measure(sex):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    addon_utils.enable('bl_ext.user_default.mpfb', default_set=True, persistent=False)
    from bl_ext.user_default.mpfb.services.humanservice import HumanService
    from bl_ext.user_default.mpfb.services.targetservice import TargetService
    m = TargetService.get_default_macro_info_dict(); m.update({'gender': 1.0 if sex == 'm' else 0.0})
    b = HumanService.create_human(scale=0.1, macro_detail_dict=m)
    arm = HumanService.add_builtin_rig(b, 'mixamo_unity')
    W = arm.matrix_world; B = arm.data.bones
    H = lambda n: W @ B[P + n].head_local; T = lambda n: W @ B[P + n].tail_local
    zs = [(b.matrix_world @ v.co).z for v in b.data.vertices]; height = max(zs) - min(zs)
    out = {'standingHeightM': round(height, 3)}
    for s, sx in (('Left', 1), ('Right', -1)):
        a = (H(s + 'ForeArm') - H(s + 'Arm')); f = (H(s + 'Hand') - H(s + 'ForeArm')); hand = (T(s + 'Hand') - H(s + 'Hand'))
        out[s] = {
            'armDownFromHorizontal': deg(math.atan2(-a.z, math.hypot(a.x, a.y))),      # upper arm below horizontal
            'armForward': deg(math.atan2(-a.y, abs(a.x))),                              # + = the arm reaches forward of the shoulder line
            'shoulderToWristDownFromHorizontal': deg(math.atan2(-(H(s + 'Hand') - H(s + 'Arm')).z, math.hypot((H(s + 'Hand') - H(s + 'Arm')).x, (H(s + 'Hand') - H(s + 'Arm')).y))),
            'elbowBend': deg(a.angle(f)),
            'wristBend': deg(f.angle(hand)),
            'fingerSpread': deg((H(s + 'HandIndex2') - H(s + 'HandIndex1')).angle(H(s + 'HandPinky2') - H(s + 'HandPinky1'))),
            'thumbFromIndex': deg((H(s + 'HandThumb2') - H(s + 'HandThumb1')).angle(H(s + 'HandIndex2') - H(s + 'HandIndex1'))),
            'legOutFromVertical': deg(math.atan2(sx * (H(s + 'Foot') - H(s + 'UpLeg')).x, -(H(s + 'Foot') - H(s + 'UpLeg')).z)),
            'kneeBend': deg((H(s + 'Leg') - H(s + 'UpLeg')).angle(H(s + 'Foot') - H(s + 'Leg'))),
        }
        # palm: the normal of the plane through index and pinky knuckles and the wrist, turned to the palm side (away from the back of the hand: the thumb's side of the curl)
        w, i1, p1 = H(s + 'Hand'), H(s + 'HandIndex1'), H(s + 'HandPinky1')
        n = (i1 - w).cross(p1 - w).normalized() * (1 if s == 'Left' else -1)
        out[s]['palmNormal'] = [round(x, 2) for x in n]
        ft = (H(s + 'ToeBase') - H(s + 'Foot'))
        out[s]['footYawOut'] = deg(math.atan2(sx * ft.x, -ft.y))                       # + = toe turned out
        out[s]['footPitchDown'] = deg(math.atan2(-ft.z, math.hypot(ft.x, ft.y)))
    hipw = (H('LeftUpLeg') - H('RightUpLeg')).length; ank = (H('LeftFoot') - H('RightFoot')); toe = (H('LeftToeBase') - H('RightToeBase'))
    sh = (H('LeftArm') - H('RightArm')).length
    out['hipJointWidthM'] = round(hipw, 3); out['ankleSpacingM'] = round(abs(ank.x), 3); out['toeSpacingM'] = round(abs(toe.x), 3); out['shoulderWidthM'] = round(sh, 3)
    out['ankleSpacingOverHipJoints'] = round(abs(ank.x) / hipw, 2)
    out['fractionOfHeight'] = {k: round(out[k] / height, 3) for k in ('hipJointWidthM', 'ankleSpacingM', 'toeSpacingM', 'shoulderWidthM')}
    nk = H('Head') - H('Neck'); hd = T('Head') - H('Head')
    out['neckForwardFromVertical'] = deg(math.atan2(-nk.y, nk.z)); out['headForwardFromVertical'] = deg(math.atan2(-hd.y, hd.z))
    sp = H('Neck') - H('Hips'); out['trunkForwardFromVertical'] = deg(math.atan2(-sp.y, sp.z))
    out['wristHeightFraction'] = round(H('LeftHand').z / height, 3); out['shoulderHeightFraction'] = round(H('LeftArm').z / height, 3)
    out['handOutFromBodyCentreFraction'] = round(H('LeftHand').x / height, 3)
    return out


res = {'schema': 'suborned.charpipe.base-pose/1', 'note': __doc__.split('\n')[0], 'm': measure('m'), 'f': measure('f')}
json.dump(res, open(sys.argv[sys.argv.index('--') + 1], 'w'), indent=1); print(json.dumps(res, indent=1))
