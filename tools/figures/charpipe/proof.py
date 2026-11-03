"""(brief 069 copy of tools/figures/proof.py: the same render, with an unswung torso in the retarget for 52-bone charpipe figures; everything else unchanged)
True-size proof of the stand-in figures at the play camera (brief 067, Forma): what a figure looks like at 53 px, in the game's light.
  blender -b -P tools/figures/proof.py -- <spec.json>
Blender side only: it renders three scene-linear passes of a row of figures and writes them as float .npy files; tools/figures/proof_post.py (system python) composes them
over a real ground patch, applies the game's grade and writes the pictures. Not a render of the game: no Unity, no shader, one flat ground. What it shares with the game:
  camera   orthographic, 38 degrees down, yaw as asked, 28 m over 1080 px (38.571 px per metre: a 1.75 m figure is 53 px tall); Unity yaw 30 sees the street from the south-south-west
  sun      light.json: intensity 1.8, #ffe0d0, pitch 38, yaw 330 (travels toward -x and +z); Blender sun strength = 1.8 x pi (Cycles divides by pi, Unity does not)
  ambient  light.json trilight: sky #dedce0 x 0.69, equator #faeadc x 0.71, ground #e2c5a4 x 0.41, value on HSV V in sRGB as Look.Colour does; a gradient world by direction
  figures  the figure's LAST LOD (the one play zoom draws) with its own albedo, matte (smoothness 0.15), no normal map; idle pose from the library's idle clip
Spec: {out, people, yaw, pitch, ppm, w, h, sun(bool), ground [r,g,b linear albedo], pose ('idle'|'rest'), spacing, look, figures:[{id, heading, x, y, albedo?, lod?, rest?, frame?}]}
Writes <out>_figs.npy (premultiplied RGBA), <out>_shadow.npy (ground with the figures as holdout), <out>_ground.npy (ground alone)."""
import bpy, sys, os, json, math, glob
import numpy as np
from mathutils import Vector, Matrix

spec = json.load(open(sys.argv[sys.argv.index('--') + 1]))
PPM = spec.get('ppm', 1080 / 28.0); W = spec['w']; H = spec['h']
YAW = spec.get('yaw', 30.0); PITCH = spec.get('pitch', 38.0)
PEOPLE = spec['people']


def srgb_to_lin(c):
    c = np.asarray(c, dtype=np.float64)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def hexrgb(h):
    h = h.lstrip('#'); return [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]


def hsv_value(rgb, k):
    """Look.Colour: scale HSV V (sRGB) by k, as the world builder does for the ambient swatches."""
    return [min(1.0, c * k) for c in rgb]    # scaling V scales all three channels: the same


# ------------------------------------------------------------------ scene
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = spec.get('samples', 64); sc.cycles.use_denoising = False
sc.cycles.max_bounces = 4; sc.cycles.diffuse_bounces = 2; sc.cycles.glossy_bounces = 1
sc.view_settings.view_transform = 'Raw'; sc.view_settings.look = 'None'; sc.display_settings.display_device = 'sRGB'
sc.render.resolution_x = W; sc.render.resolution_y = H; sc.render.resolution_percentage = 100
sc.render.film_transparent = True
sc.render.image_settings.file_format = 'OPEN_EXR'; sc.render.image_settings.color_depth = '32'; sc.render.image_settings.color_mode = 'RGBA'

# world: a vertical gradient (ground / equator / sky) for the ambient, strength 1 (the colours are the irradiance multipliers Unity uses)
amb = spec.get('ambient') or {
    'sky': srgb_to_lin(hsv_value(hexrgb('#dedce0'), 0.69)), 'equator': srgb_to_lin(hsv_value(hexrgb('#faeadc'), 0.71)), 'ground': srgb_to_lin(hsv_value(hexrgb('#e2c5a4'), 0.41))}
w = bpy.data.worlds.new('w'); w.use_nodes = True; nt = w.node_tree
for n in list(nt.nodes): nt.nodes.remove(n)
tc = nt.nodes.new('ShaderNodeTexCoord'); sp = nt.nodes.new('ShaderNodeSeparateXYZ'); mr = nt.nodes.new('ShaderNodeMapRange')
cr = nt.nodes.new('ShaderNodeValToRGB'); bg = nt.nodes.new('ShaderNodeBackground'); out = nt.nodes.new('ShaderNodeOutputWorld')
nt.links.new(tc.outputs['Generated'], sp.inputs['Vector'])
mr.inputs['From Min'].default_value = -1.0; mr.inputs['From Max'].default_value = 1.0; mr.inputs['To Min'].default_value = 0.0; mr.inputs['To Max'].default_value = 1.0
nt.links.new(sp.outputs['Z'], mr.inputs['Value']); nt.links.new(mr.outputs['Result'], cr.inputs['Fac'])
cr.color_ramp.elements[0].position = 0.0; cr.color_ramp.elements[0].color = (*amb['ground'], 1)
cr.color_ramp.elements[1].position = 1.0; cr.color_ramp.elements[1].color = (*amb['sky'], 1)
e = cr.color_ramp.elements.new(0.5); e.color = (*amb['equator'], 1)
nt.links.new(cr.outputs['Color'], bg.inputs['Color']); bg.inputs['Strength'].default_value = 1.0; nt.links.new(bg.outputs['Background'], out.inputs['Surface'])
sc.world = w

# sun: Unity yaw 330 pitch 38 -> travel direction in Unity (sin yaw cos p, -sin p, cos yaw cos p); Blender = (x, z, y)
SUN_ON = spec.get('sun', True)
sy, sp_ = math.radians(spec.get('sunYaw', 330.0)), math.radians(spec.get('sunPitch', 38.0))
travel = Vector((math.sin(sy) * math.cos(sp_), math.cos(sy) * math.cos(sp_), -math.sin(sp_)))
sl = bpy.data.lights.new('sun', 'SUN'); SUN_E = spec.get('sunIntensity', 1.8) * math.pi; sl.energy = SUN_E if SUN_ON else 0.0
sl.color = tuple(srgb_to_lin(hexrgb('#ffe0d0'))); sl.angle = math.radians(1.0)
so = bpy.data.objects.new('sun', sl); sc.collection.objects.link(so); so.rotation_euler = travel.to_track_quat('-Z', 'Y').to_euler()

# camera: orthographic, Unity yaw/pitch -> Blender direction (x, z, y)
cy, cp = math.radians(YAW), math.radians(PITCH)
cdir = Vector((math.sin(cy) * math.cos(cp), math.cos(cy) * math.cos(cp), -math.sin(cp)))
right = Vector((math.cos(cy), -math.sin(cy), 0.0))
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.sensor_fit = 'VERTICAL'; cam.data.ortho_scale = H / PPM; cam.data.clip_start = 0.1; cam.data.clip_end = 400
centre = Vector((0.0, 0.0, 0.0)) + Vector(spec.get('look', [0.0, 0.0, 0.0]))
cam.location = centre - cdir * 100.0; cam.rotation_euler = cdir.to_track_quat('-Z', 'Y').to_euler()
sc.camera = cam

# ground: a big plane, diffuse, albedo as asked
g = spec.get('ground', [0.25, 0.28, 0.30])
bpy.ops.mesh.primitive_plane_add(size=400, location=(0, 0, 0)); ground = bpy.context.active_object; ground.name = 'ground'
gm = bpy.data.materials.new('ground'); gm.use_nodes = True; gb = gm.node_tree.nodes['Principled BSDF']
gb.inputs['Base Color'].default_value = (*g, 1); gb.inputs['Roughness'].default_value = 1.0; gb.inputs['Specular IOR Level'].default_value = 0.0
ground.data.materials.append(gm)


# ------------------------------------------------------------------ figures
def bare(n): return n.split(':')[-1]


def play_lod(meshes, fid, lod):
    mm = sorted([o for o in meshes if '_LOD' in o.name], key=lambda o: int(o.name.rsplit('_LOD', 1)[1].split('.')[0]))
    if not mm: return meshes
    pick = mm[-1] if lod is None or lod < 0 else mm[min(lod, len(mm) - 1)]
    for o in mm:
        if o is not pick: bpy.data.objects.remove(o, do_unlink=True)
    return [pick]


def bind(o, fid, albedo_override):
    """One material per slot: the slot's albedo from People/Figures/<id>/Textures/<material>_albedo.*; an emissive map lights it; the override replaces the body albedo."""
    tdir = f'{PEOPLE}/Figures/{fid}/Textures/'
    for si, slot in enumerate(o.material_slots):
        name = slot.material.name.split('.')[0] if slot.material else f'{fid}_body_512'
        m = bpy.data.materials.new(f'{name}_proof'); m.use_nodes = True; nt = m.node_tree
        for n in list(nt.nodes): nt.nodes.remove(n)
        b = nt.nodes.new('ShaderNodeBsdfPrincipled'); ot = nt.nodes.new('ShaderNodeOutputMaterial')
        b.inputs['Roughness'].default_value = 0.85; b.inputs['Specular IOR Level'].default_value = 0.35
        v = spec.get('veil', 0.0)     # the game's reflection probes put a pale veil over dark cloth (smoothness 0.15 still mirrors the bright street canyon): a small flat emission stands in for it
        if v: b.inputs['Emission Color'].default_value = (0.92, 0.92, 0.97, 1); b.inputs['Emission Strength'].default_value = v
        nt.links.new(b.outputs['BSDF'], ot.inputs['Surface'])
        alb = albedo_override if (albedo_override and '_body_' in name) else next(iter(sorted(glob.glob(tdir + name + '_albedo.*'))), None)
        if alb:
            t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(alb); t.image.colorspace_settings.name = 'sRGB'
            nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
        em = next(iter(sorted(glob.glob(tdir + name + '_emissive.*'))), None)
        if em:
            t2 = nt.nodes.new('ShaderNodeTexImage'); t2.image = bpy.data.images.load(em); nt.links.new(t2.outputs['Color'], b.inputs['Emission Color']); b.inputs['Emission Strength'].default_value = 1.0
        o.material_slots[si].material = m


def retarget_pose(fig_arm, clip_file, src_frame):
    """Pose fig_arm as the Mixamo clip is at src_frame (tv_render.py's retarget, one frame, no keys): armature-space rotation deltas with a swing correction for the A-pose rig."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=clip_file)
    new = [o for o in bpy.data.objects if o not in before]
    carm = [o for o in new if o.type == 'ARMATURE'][0]
    cb = {bare(b.name): b for b in carm.data.bones}; fb = {b.name: b for b in fig_arm.data.bones}
    def depth(b): return 0 if b.parent is None else 1 + depth(b.parent)
    order = [b.name for b in sorted(fig_arm.data.bones, key=depth) if b.name in cb]
    cmap = {bare(x): x for x in carm.pose.bones.keys()}
    CW = carm.matrix_world; FW = fig_arm.matrix_world
    c_rest = {n: CW @ cb[n].matrix_local for n in order}
    f_rest_a = {n: fb[n].matrix_local.copy() for n in order}
    f_rest_w = {n: FW @ f_rest_a[n] for n in order}
    Cn = {}
    major = ('Spine', 'Spine1', 'Spine2', 'LeftLeg', 'RightLeg', 'LeftForeArm', 'RightForeArm', 'LeftHand', 'RightHand', 'LeftFoot', 'RightFoot', 'Head', 'Neck', 'LeftArm', 'RightArm', 'LeftToeBase', 'RightToeBase', 'LeftUpLeg', 'RightUpLeg')
    upright = len(fig_arm.data.bones) == 52 and 'LeftHandIndex1' in fb   # brief 069: a charpipe figure (MPFB rig): its torso and neck stand upright in rest as the clip's do, so they take the clip's turn unswung (the child swing tipped Spine2 and Neck 16 degrees and bowed the head)
    for n in order:
        kids = [k.name for k in fb[n].children if k.name in cb and k.name in fb]
        if upright and n in ('Hips', 'Spine', 'Spine1', 'Spine2', 'Neck', 'Head', 'LeftShoulder', 'RightShoulder'):
            Cn[n] = Matrix.Identity(3)
        elif kids:
            k = [x for x in kids if x in major]; k = (k or kids)[0] if n != 'Hips' else 'Spine'
            df = (f_rest_w[k].translation - f_rest_w[n].translation).normalized(); dc = (c_rest[k].translation - c_rest[n].translation).normalized()
            Cn[n] = df.rotation_difference(dc).to_matrix()
        else:
            Cn[n] = Cn[fb[n].parent.name] if fb[n].parent and fb[n].parent.name in Cn else Matrix.Identity(3)
    ratio = f_rest_w['Hips'].translation.z / c_rest['Hips'].translation.z
    for pb in fig_arm.pose.bones: pb.rotation_mode = 'QUATERNION'
    sc.frame_set(src_frame)
    FWi = FW.inverted(); pose = {}
    cpw = {n: CW @ carm.pose.bones[cmap[n]].matrix for n in order}
    for n in order:
        D = cpw[n].to_3x3() @ c_rest[n].to_3x3().inverted()
        rot_w = D @ Cn[n] @ f_rest_w[n].to_3x3(); rot_a = FWi.to_3x3() @ rot_w
        par = fb[n].parent
        if par is None or par.name not in pose:
            dw = cpw[n].translation - c_rest[n].translation
            head_a = FWi @ (f_rest_w[n].translation + dw * ratio)
        else:
            rel = f_rest_a[par.name].inverted() @ f_rest_a[n]; head_a = (pose[par.name] @ rel).translation
        M = Matrix.Translation(head_a) @ rot_a.to_4x4(); pose[n] = M
        if par is None or par.name not in pose: basis = f_rest_a[n].inverted() @ M
        else:
            rel = f_rest_a[par.name].inverted() @ f_rest_a[n]; basis = rel.inverted() @ pose[par.name].inverted() @ M
        pb = fig_arm.pose.bones[n]; loc, q, _ = basis.decompose(); pb.location = loc; pb.rotation_quaternion = q
    for o in new: bpy.data.objects.remove(o, do_unlink=True)
    bpy.context.view_layer.update()


figs = []
n = len(spec['figures']); spacing = spec.get('spacing', 1.0)
for i, f in enumerate(spec['figures']):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=f'{PEOPLE}/Figures/{f["id"]}/{f["id"]}.fbx')
    new = [o for o in bpy.data.objects if o not in before]
    arm = next(o for o in new if o.type == 'ARMATURE'); meshes = [o for o in new if o.type == 'MESH']
    if arm.animation_data: arm.animation_data_clear()      # the cell's FBXs carry their clips: a frame change would put the object back at the origin
    keep = play_lod(meshes, f['id'], f.get('lod'))
    for o in keep: bind(o, f['id'], f.get('albedo'))
    if spec.get('pose', 'idle') == 'idle' and not f.get('rest'):
        retarget_pose(arm, f'{PEOPLE}/Clips/{f.get("clip", "idle")}.fbx', int(f.get('frame', 4)))
    heading = f.get('heading', YAW + 180.0)
    fwd = Vector((math.sin(cy), math.cos(cy), 0.0))      # along the ground, away from the camera
    pos = right * ((i - (n - 1) / 2.0) * spacing + f.get('x', 0.0)) + fwd * f.get('y', 0.0)   # x: along the screen's right, y: up the screen over the ground (metres); spacing sets the row
    pos.z = 0.0
    arm.location = pos; arm.rotation_euler = (0, 0, math.radians(180.0 - heading))
    for o in keep: o.hide_render = False
    figs.append((arm, keep))
bpy.context.view_layer.update()
allfig = [o for arm, ms in figs for o in [arm] + ms]


def render(name, ground_holdout, figs_visible, figs_holdout):
    """One pass to <out>_<name>.npy (scene-linear float RGBA, top row first, premultiplied): the ground is always there to light and shade; a holdout is there but not in the picture."""
    ground.hide_render = False; ground.is_holdout = ground_holdout
    for o in allfig:
        o.hide_render = not figs_visible
        if o.type == 'MESH': o.is_holdout = figs_holdout
    path = spec['out'] + f'_{name}.exr'; sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path); px = np.array(img.pixels[:], dtype=np.float32).reshape(H, W, 4)[::-1].copy(); bpy.data.images.remove(img)
    np.save(spec['out'] + f'_{name}.npy', px); os.remove(path)
    print('PASS', name, 'mean alpha', round(float(px[..., 3].mean()), 4))


render('ground', False, False, False)    # the ground alone, lit
render('shadow', False, True, True)      # the ground with the figures' shadows (the figures themselves held out)
render('figs', True, True, False)        # the figures, lit by the sun, the sky and the ground's bounce (the ground held out)
