"""Build shared-material runtime LODs without modifying the editable v2 source."""
import bpy
import json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
SOURCE = ROOT / 'ArtSource/civic-control-cabinet/v2/civic-service-cabinet-c.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
sources = [o for o in bpy.context.scene.objects if o.type == 'MESH']
OUT.mkdir(exist_ok=True)
(OUT / 'textures').mkdir(exist_ok=True)

def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

# Separate files leave Unity free to set platform compression. All LODs refer
# to the same six maps; the untouched 2k authoring maps remain in v2.
for image in bpy.data.images:
    if not image.users:
        continue
    if max(image.size) > 1024:
        image.scale(1024, 1024)
    image.file_format = 'PNG'
    path = OUT / 'textures' / (image.name + '.png')
    # A new image avoids both display transforms and passing through the
    # original packed JPEG buffer after resizing.
    pixels = list(image.pixels)
    width, height = image.size
    name, space = image.name, image.colorspace_settings.name
    fresh = bpy.data.images.new(name + '_Runtime', width=width, height=height)
    fresh.colorspace_settings.name = space
    fresh.pixels = pixels
    fresh.file_format = 'PNG'
    fresh.filepath_raw = str(path)
    fresh.save()
    image.user_remap(fresh)
    bpy.data.images.remove(image)
    fresh.pack()

report = []
for level, ratio in enumerate([1, .42, .12]):
    root = bpy.data.objects.new('LOD' + str(level), None)
    bpy.context.collection.objects.link(root)
    root['lod_level'] = level
    parts = []
    for source in sources:
        obj = source.copy()
        obj.data = source.data.copy()
        bpy.context.collection.objects.link(obj)
        obj.name = 'LOD%d_%s' % (level, source.name)
        activate(obj)
        # Flat skins and the watertight backing are inexpensive and should
        # never collapse. Simplify only the dense shell and raised hardware.
        protected = source.name in {'Cabinet_ClosedInnerCasing', 'Door_BlueSkin',
                                    'Controls_PearlSkin', 'Door_RightGasket'}
        if level and not protected:
            mod = obj.modifiers.new('Runtime_Simplification', 'DECIMATE')
            mod.ratio = ratio
            mod.use_collapse_triangulate = True
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.data.validate()
        parts.append(obj)
    # Merge by material only after simplification; original assemblies remain
    # independently editable in the authoring source.
    groups = {}
    for obj in parts:
        groups.setdefault(obj.data.materials[0], []).append(obj)
    for material, group in groups.items():
        activate(group[0])
        for obj in group:
            obj.select_set(True)
        if len(group) > 1:
            bpy.ops.object.join()
        obj = bpy.context.object
        obj.name = 'LOD%d_%s' % (level, material.name)
        obj.parent = root
    meshes = list(root.children)
    vertices, faces = [], []
    for obj in meshes:
        offset = len(vertices)
        vertices.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        faces.extend(tuple(offset + i for i in p.vertices) for p in obj.data.polygons)
    tree = BVHTree.FromPolygons(vertices, faces)
    rays = 0
    for side in [-1, 1]:
        for ix in range(24):
            for iz in range(24):
                x, z = -.47 + ix*.94/23, .15 + iz*.75/23
                hit = tree.ray_cast(Vector((x, side*2, z)), Vector((0, -side, 0)))[0]
                if hit is None or hit.y*side < .1:
                    raise RuntimeError('Open casing at LOD%d (%s, %s, %s)' % (level, x, z, side))
                rays += 1
    report.append({'level': level, 'triangles': sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),
                   'meshes': len(meshes), 'surface_rays_passed': rays})

for obj in sources:
    bpy.data.objects.remove(obj, do_unlink=True)
bpy.context.view_layer.update()
for obj in bpy.context.scene.objects:
    if obj.parent and obj.parent.name in {'LOD1', 'LOD2'}:
        obj.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'civic-service-cabinet-c-lods.blend'))
for obj in bpy.context.scene.objects:
    obj.hide_set(False)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(OUT / 'civic-service-cabinet-c-lods.glb'),
    export_format='GLB', use_selection=True, export_apply=True, export_animations=False,
    export_extras=True, export_yup=True)
bpy.ops.export_scene.fbx(filepath=str(OUT / 'civic-service-cabinet-c-lods.fbx'),
    use_selection=True, object_types={'MESH', 'EMPTY'}, path_mode='COPY',
    embed_textures=True, bake_anim=False, axis_forward='-Z', axis_up='Y')
result = {'source': str(SOURCE.relative_to(ROOT)), 'levels': report,
          'textures': [{'name': i.name, 'size': list(i.size)} for i in bpy.data.images if i.users],
          'height_metres': 1.05, 'paid_generation': False, 'unity_verified': False,
          'unity_setup': 'Assign LOD0/1/2 renderers to one Unity LODGroup; bind PBR maps for FBX.'}
(OUT / 'lods.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result))
