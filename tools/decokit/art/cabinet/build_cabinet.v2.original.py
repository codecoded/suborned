"""Blender-authored hardware repair. Run with Blender --background --python."""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
SOURCE = ROOT / 'ArtSource/civic-control-cabinet/v1/civic-service-cabinet-c.glb'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
bpy.context.scene.unit_settings.system = 'METRIC'
body = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
body.name = 'Cabinet_OriginalShell'
bpy.context.view_layer.update()
# Bake the import transform before localized surface surgery.
body.data.transform(body.matrix_world)
body.matrix_world.identity()
original = body.data.materials[0]
original.name = 'Cabinet_OriginalPBR'
created = []

def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

def family(name, roughness=None, metallic=None):
    mat = original.copy()
    mat.name = name
    bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    for slot, value in [('Roughness', roughness), ('Metallic', metallic)]:
        if value is not None:
            for link in list(bsdf.inputs[slot].links):
                mat.node_tree.links.remove(link)
            bsdf.inputs[slot].default_value = value
    normal = next((n for n in mat.node_tree.nodes if n.type == 'NORMAL_MAP'), None)
    if normal:
        normal.inputs['Strength'].default_value = .3
    return mat

blue = family('Cabinet_BlueEnamel')
pearl = family('Cabinet_PearlEnamel')
brass = family('Cabinet_SatinBrass', roughness=.38, metallic=.8)
ink = family('Cabinet_InstrumentInk', roughness=.65, metallic=.0)
amber = family('Cabinet_AmberLens', roughness=.22, metallic=.0)
# Reuse the district's gold PBR at a restrained normal strength. These small
# hardware maps are 256px; large cabinet surfaces still share the source atlas.
nodes = brass.node_tree.nodes
links = brass.node_tree.links
bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
for node in list(nodes):
    if node.type not in {'BSDF_PRINCIPLED','OUTPUT_MATERIAL'}:
        nodes.remove(node)
gold_sources = []
for kind in ['base','normal','orm']:
    source = ROOT / 'experiments/civic-2p5d/art/district-road' / ('gold-'+kind+'-v2.webp')
    image = bpy.data.images.load(str(source),check_existing=True)
    image.name = 'SharedGold_'+kind
    image.colorspace_settings.name = 'sRGB' if kind=='base' else 'Non-Color'
    image.scale(256,256)
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = image
    gold_sources.append(str(source.relative_to(ROOT)))
    if kind=='base':
        links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
    elif kind=='normal':
        normal = nodes.new('ShaderNodeNormalMap')
        normal.inputs['Strength'].default_value=.16
        links.new(tex.outputs['Color'],normal.inputs['Color'])
        links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
    else:
        channels = nodes.new('ShaderNodeSeparateColor')
        links.new(tex.outputs['Color'],channels.inputs['Color'])
        links.new(channels.outputs['Green'],bsdf.inputs['Roughness'])
# Texture coordinates reference clean regions of the existing Meshy atlas.
# The original three embedded maps are shared, never duplicated per component.
patches = {
    blue.name: (380/2048, 350/2048, 210/2048, 190/2048),
    pearl.name: (1810/2048, 550/2048, 95/2048, 90/2048),
    brass.name: (0,0,1,1),
    ink.name: (160/2048, 215/2048, 50/2048, 60/2048),
    amber.name: (814/2048, 150/2048, 7/2048, 7/2048),
}

def map_surface(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    uv = obj.data.uv_layers.active or obj.data.uv_layers.new(name='UVMap')
    coords = [v.co for v in obj.data.vertices]
    lo = [min(v[i] for v in coords) for i in range(3)]
    hi = [max(v[i] for v in coords) for i in range(3)]
    u, top, w, h = patches[mat.name]
    for p in obj.data.polygons:
        # Front/back use X/Z; narrow side walls use their longest dimensions.
        axes = sorted(range(3), key=lambda i: hi[i] - lo[i], reverse=True)
        a, b = (0, 2) if abs(p.normal.y) > .6 else axes[:2]
        for index in p.loop_indices:
            v = obj.data.vertices[obj.data.loops[index].vertex_index].co
            s = (v[a]-lo[a]) / max(hi[a]-lo[a], 1e-8)
            t = (v[b]-lo[b]) / max(hi[b]-lo[b], 1e-8)
            uv.data[index].uv = (u+s*w, 1-top-h+t*h)
    return obj

def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    map_surface(obj, mat)
    created.append(obj)
    return obj

def box(name, center, dimensions, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    activate(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Machined_edge', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    map_surface(obj, mat)
    created.append(obj)
    return obj

def ellipse(rx, rz, count=32):
    return [(rx*math.cos(i*2*math.pi/count), rz*math.sin(i*2*math.pi/count)) for i in range(count)]

def rounded_rect(rx, rz, radius, steps=4):
    points = []
    for cx, cz, start in [(rx-radius, rz-radius, 0), (-rx+radius, rz-radius, 90),
                           (-rx+radius, -rz+radius, 180), (rx-radius, -rz+radius, 270)]:
        for i in range(steps+1):
            angle = math.radians(start + i*90/steps)
            points.append((cx+radius*math.cos(angle), cz+radius*math.sin(angle)))
    return points

def profile(name, center, loops, mat, caps=False):
    # Each loop is a front-facing X/Z contour and a Y-depth; supports open bezels.
    cx, cz = center
    count = len(loops[0][0])
    verts = [(cx+x, y, cz+z) for contour, y in loops for x, z in contour]
    faces = []
    for j in range(len(loops)-1):
        for i in range(count):
            a = j*count+i
            b = j*count+(i+1)%count
            faces.append((a,b,b+count,a+count))
    if caps:
        faces.extend([tuple(reversed(range(count))), tuple(range((len(loops)-1)*count,len(loops)*count))])
    obj = mesh(name, verts, faces, mat)
    for p in obj.data.polygons:
        p.use_smooth = len(p.vertices) == 4
    return obj

def disc(name, x, z, rx, rz, back, front, mat, count=32):
    return profile(name, (x,z), [(ellipse(rx,rz,count),back),
                   (ellipse(rx,rz,count),front+.002),
                   (ellipse(rx-.002,rz-.002,count),front)], mat, caps=True)

def text(name, content, x, z, size, y=-.258):
    curve = bpy.data.curves.new(name, 'FONT')
    curve.body = content
    curve.size = size
    curve.align_x = 'CENTER'
    curve.align_y = 'CENTER'
    curve.resolution_u = 2
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.location = (x,y,z)
    obj.rotation_euler = (math.pi/2,0,0)
    activate(obj)
    bpy.ops.object.convert(target='MESH')
    map_surface(obj, ink)
    created.append(obj)
    return obj

# Meshy has open geometric boundaries underneath its UV/normal splits. A solid
# boolean can classify the rear as inside the front cutter. Split faces at the
# repair bounds and delete only front-facing-region geometry instead.
bm = bmesh.new()
bm.from_mesh(body.data)
for axis, coordinate in [(0,-.414),(0,.434),(2,-.334),(2,.318),(1,-.190)]:
    point, normal = Vector(), Vector()
    point[axis] = coordinate
    normal[axis] = 1
    bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
        dist=.0000001,plane_co=point,plane_no=normal)
front_faces = [f for f in bm.faces if -.414 < f.calc_center_median().x < .434
               and -.334 < f.calc_center_median().z < .318
               and f.calc_center_median().y < -.190]
bmesh.ops.delete(bm,geom=front_faces,context='FACES')
bm.to_mesh(body.data)
bm.free()
body.data.update()

# A closed casing overlaps the frame, roof and plinth from inside. Deliberate
# panel reveals can remain visible without looking through to the far side.
inner_case = box('Cabinet_ClosedInnerCasing', (.001,.000,-.008),
                 (.894,.450,.720),blue,.004)

door = box('Door_BlueSkin', (-.107,-.223,-.010), (.618,.026,.648), blue, .006)
spine = box('Controls_PearlSkin', (.320,-.220,-.0045), (.228,.026,.659), pearl, .005)
# A small dark reveal separates the working door from the fixed control spine.
box('Door_RightGasket', (.203,-.230,-.010), (.005,.005,.637), ink)

# Gauge: symmetric rolled oval brass bezel, recessed ivory face, calibrated
# scale and a physical needle. The face is glossy enamel behind the raised rim.
gx, gz = .325, .218
profile('Gauge_Bezel', (gx,gz), [
    (ellipse(.076,.050),-.233), (ellipse(.077,.051),-.240),
    (ellipse(.075,.049),-.255), (ellipse(.071,.045),-.259),
    (ellipse(.067,.041),-.256), (ellipse(.066,.040),-.244)], brass)
disc('Gauge_Gasket', gx,gz,.068,.042,-.243,-.251,ink)
disc('Gauge_Face', gx,gz,.065,.039,-.246,-.253,pearl)
for i in range(21):
    angle = math.radians(160-i*7)
    major = i%5 == 0
    r1 = .034 if major else .037
    r2 = .041
    dx,dz = math.cos(angle),math.sin(angle)
    # Squashed meter scale follows the instrument face, not the exterior bezel.
    p1 = (gx+dx*r1*1.4, -.256, gz-.019+dz*r1)
    p2 = (gx+dx*r2*1.4, -.256, gz-.019+dz*r2)
    tangent = Vector((-dz*.0007,0,dx*.0007))
    a,b = Vector(p1),Vector(p2)
    mesh('Gauge_ScaleTick', [a-tangent,a+tangent,b+tangent,b-tangent], [(0,1,2,3)], ink)
for label, angle in [('0',160),('150',90),('300',20)]:
    a = math.radians(angle)
    text('Gauge_ScaleLabel', label, gx+math.cos(a)*.038, gz-.017+math.sin(a)*.024, .007)
text('Gauge_Units', 'V', gx,gz-.007,.010)
mesh('Gauge_Needle', [(gx-.003,-.260,gz-.022),(gx+.002,-.260,gz-.022),
                     (gx+.020,-.260,gz+.015)], [(0,1,2)], ink)
disc('Gauge_Pivot',gx,gz-.022,.005,.005,-.257,-.261,brass,20)

# Selector: circular mounting plate with deliberate stepped concentric rim,
# rounded paddle, indexing marker and a three-position arc.
sx, sz = .325, .064
profile('Selector_Bezel', (sx,sz), [(ellipse(.052,.052),-.233),
    (ellipse(.053,.053),-.239),(ellipse(.050,.050),-.246),
    (ellipse(.045,.045),-.248),(ellipse(.043,.043),-.242)], brass)
disc('Selector_Reveal', sx,sz,.044,.044,-.239,-.245,ink)
disc('Selector_Hub', sx,sz,.039,.039,-.243,-.251,brass)
profile('Selector_Paddle', (sx,sz), [
    (rounded_rect(.012,.035,.009),-.250),
    (rounded_rect(.013,.035,.009),-.262),
    (rounded_rect(.010,.032,.008),-.276)], brass, caps=True)
box('Selector_PearlIndex', (sx,-.277,sz+.025), (.004,.001,.009), pearl,.001)
for a in [45,90,135]:
    angle = math.radians(a)
    disc('Selector_IndexDot', sx+.060*math.cos(angle),sz+.060*math.sin(angle),
         .002,.002,-.233,-.235,ink,12)

# Flush latch lies wholly on the moving blue door, clear of the fixed spine.
hx, hz = .143, -.030
profile('Latch_Frame',(hx,hz), [
    (rounded_rect(.033,.073,.008),-.236),
    (rounded_rect(.033,.073,.008),-.245),
    (rounded_rect(.030,.070,.007),-.250),
    (rounded_rect(.025,.065,.006),-.249),
    (rounded_rect(.024,.064,.005),-.237)], brass)
profile('Latch_Recess',(hx,hz), [
    (rounded_rect(.024,.064,.005),-.237),
    (rounded_rect(.024,.064,.005),-.242)],ink,caps=True)
profile('Latch_Pull',(hx+.006,hz+.008), [
    (rounded_rect(.011,.049,.008),-.242),
    (rounded_rect(.011,.049,.008),-.259),
    (rounded_rect(.008,.046,.006),-.265)],brass,caps=True)
disc('Latch_KeyCylinder',hx,hz-.049,.007,.007,-.243,-.249,brass,24)
box('Latch_KeySlot',(hx,-.251,hz-.049),(.002,.001,.007),ink,.0003)

# Existing lenses get clean circular housings while the mounting skin is rebuilt.
for z, mat, name in [(-.037,amber,'Amber'),(-.101,pearl,'Pearl')]:
    profile('Status_'+name+'_Rim',(.325,z),[(ellipse(.022,.022,32),-.233),
        (ellipse(.022,.022,32),-.242),(ellipse(.018,.018,32),-.246)],brass)
    disc('Status_'+name+'_Lens',.325,z,.017,.017,-.241,-.248,mat,32)

# Keep semantic assemblies editable, but merge same-material subparts per
# assembly for export so scale ticks do not become dozens of draw calls.
for prefix in ['Gauge_', 'Selector_', 'Latch_', 'Status_']:
    for mat in [brass,pearl,ink,amber]:
        parts = [o for o in bpy.context.scene.objects if o.type == 'MESH'
                 and o.name.startswith(prefix) and o.data.materials[0] == mat]
        if not parts:
            continue
        activate(parts[0])
        for obj in parts:
            obj.select_set(True)
        if len(parts)>1:
            bpy.ops.object.join()
        bpy.context.object.name = prefix+mat.name.removeprefix('Cabinet_')

# Author in metres, bottom origin, Y-up on GLB/FBX export.
scale = 1.05/.9332430064678192
for obj in list(bpy.context.scene.objects):
    if obj.type != 'MESH':
        continue
    obj.data.transform(obj.matrix_world)
    obj.matrix_world.identity()
    for v in obj.data.vertices:
        v.co.x *= scale
        v.co.y = (v.co.y-.00253300368785858)*scale
        v.co.z = (v.co.z+.4696179926395416)*scale
    obj['authoring'] = 'Blender hardware repair v2'
    obj['source'] = 'Meshy T2 v1; original PBR atlas reused'
bpy.context.view_layer.update()
OUT.mkdir(parents=True,exist_ok=True)
# Preserve the GLB's embedded JPEG buffers byte-for-byte. Newly resized library
# maps get lossless PNG files, with correct extensions for FBX engine import.
texture_dir = OUT/'textures'
texture_dir.mkdir(exist_ok=True)
for image in bpy.data.images:
    if image.packed_file:
        path = str(texture_dir/(image.name+'.jpg'))
        Path(path).write_bytes(image.packed_file.data)
        image.unpack(method='REMOVE')
    else:
        path = str(texture_dir/(image.name+'.png'))
        image.file_format = 'PNG'
        image.save(filepath=path)
    image.filepath = path
    image.reload()
    image.pack()
bpy.context.scene.tool_settings.transform_pivot_point = 'MEDIAN_POINT'
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.clip_start=.01
            area.spaces.active.region_3d.view_distance=2.4
            area.spaces.active.region_3d.view_location=(0,0,.525)
            area.spaces.active.region_3d.view_perspective='ORTHO'
            from mathutils import Euler
            area.spaces.active.region_3d.view_rotation=Euler((math.radians(75),0,math.radians(-20))).to_quaternion()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'civic-service-cabinet-c.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(OUT/'civic-service-cabinet-c.glb'),
    export_format='GLB',use_selection=True,export_apply=True,export_animations=False,
    export_extras=True,export_yup=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'civic-service-cabinet-c.fbx'),
    use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,
    path_mode='COPY',embed_textures=True,bake_anim=False,axis_forward='-Z',axis_up='Y')
objects = [o for o in bpy.context.scene.objects if o.type=='MESH']
result = {
    'source': str(SOURCE.relative_to(ROOT)), 'blender': bpy.app.version_string,
    'changes': ['Front-only trimming preserves original rear surfaces', 'Closed inner casing backs all panel joins',
                'Rebuilt clean front skins with original atlas', 'Oval instrument bezel, scale and needle',
                'Stepped rotary selector and shaped paddle', 'Recessed door-only latch and keyway', 'Clean status lens housings'],
    'objects': [{'name':o.name,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),
                 'materials':[m.name for m in o.data.materials],
                 'uv_layers':[u.name for u in o.data.uv_layers]} for o in objects],
    'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects),
    'materials': sorted({m.name for o in objects for m in o.data.materials}),
    'texture_images': [{'name':i.name,'dimensions':list(i.size)} for i in bpy.data.images if i.users],
    'shared_gold_sources':gold_sources,
    'height_m': 1.05, 'paid_generation':False, 'unity_verified':False,
    'limits':['Closed prop; no working door or rig', 'Original outer shell retained rather than fully retopologized',
              'FBX requires engine-side PBR map binding; GLB retains packed roughness/metallic'],
}
with (OUT/'repair.json').open('w') as f:
    json.dump(result,f,indent=2)
print(json.dumps(result))
