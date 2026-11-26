"""Build the Lantern Terraces pendant in Blender. Arguments: export.fbx and an optional source.blend."""
import math
from pathlib import Path
import sys
import bpy
from mathutils import Euler, Vector

TAU = 2 * math.pi
args = sys.argv[sys.argv.index('--') + 1:]
output = Path(args[0]).resolve()
bpy.ops.wm.read_factory_settings(use_empty=True)

def material(name, rgb, metal, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Metallic'].default_value = metal
    bsdf.inputs['Roughness'].default_value = roughness
    return mat

surfaces = [
    material('A_Opal', (.93,.76,.46), 0, .23),
    material('B_Champagne', (.68,.45,.19), .87, .25),
    material('C_Enamel', (.042,.24,.20), .24, .26),
]

def make(name, segments, tube_sides, source=False):
    vertices, faces, finishes, smooth = [], [], [], []
    def rings(sections, finish, loop=False):
        first = len(vertices); count = len(sections[0])
        for row in sections:
            vertices.extend(row)
        for j in range(len(sections) if loop else len(sections)-1):
            nxt = (j+1) % len(sections)
            for k in range(count):
                t=(k+1)%count
                faces.append((first+j*count+k,first+j*count+t,first+nxt*count+t,first+nxt*count+k))
                finishes.append(finish);smooth.append(True)
    def lathe(profile,finish,flutes=0):
        sections=[]
        for z,r in profile:
            row=[]
            for k in range(segments):
                a=TAU*k/segments
                rr=max(.0005,r)*(1+.058*math.cos(a*flutes) if flutes else 1)
                row.append((rr*math.cos(a),rr*math.sin(a),z))
            sections.append(row)
        rings(sections,finish)
    # Each profile runs from the underside to the top. Shared normals soften the blown glass,
    # while paired profile rings hold the highlights along the rolled metal edges.
    n=max(6,segments//3)
    lathe([(-1.37+.62*math.sin(-math.pi/2+math.pi*j/n), .82*math.cos(-math.pi/2+math.pi*j/n)) for j in range(n+1)],0)
    lathe([(-1.405,.797),(-1.399,.830),(-1.384,.858),(-1.35,.858),(-1.333,.83),(-1.331,.797)],1)
    lathe([(-.829,.41),(-.811,.455),(-.784,.445),(-.758,.387),(-.742,.385),(-.733,.35),(-.682,.344),(-.660,.306),(-.649,.265),(-.617,.261),(-.592,.19),(-.583,.112)],2)
    lathe([(-.826,.424),(-.816,.46),(-.795,.463),(-.785,.431)],1)
    lathe([(-.595,.107),(-.575,.125),(-.548,.105),(-.531,.082),(-.103,.082),(-.086,.106),(-.055,.110),(-.044,.084),(0,.079)],1, 8 if segments>=24 else 0)
    lathe([(-2.076,.087),(-2.065,.124),(-2.028,.159),(-2.006,.202),(-1.981,.19),(-1.957,.122)],2)
    lathe([(-2.35,.001),(-2.27,.037),(-2.17,.071),(-2.123,.096),(-2.087,.061),(-2.062,.091)],1)
    def orbit(x_angle,z_angle):
        rot=Euler((math.radians(x_angle),0,math.radians(z_angle)),'XYZ').to_matrix()
        sections=[]
        for j in range(segments):
            a=TAU*j/segments
            radial=Vector((math.cos(a),math.sin(a),0)); top=Vector((0,0,1))
            sections.append([rot@(radial*(1.04+.025*math.cos(TAU*k/tube_sides))-top*.019*math.sin(TAU*k/tube_sides))+Vector((0,0,-1.365))
                             for k in range(tube_sides)])
        rings(sections,1,True)
    orbit(38,38);orbit(-38,-38)
    if source or segments>=36:
        for z,r in [(-.73,.354),(-.655,.304),(-2.015,.189)]:
            lathe([(z-.010,r-.008),(z-.005,r+.005),(z+.005,r+.005),(z+.010,r-.008)],1)
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj)
    for mat in surfaces:mesh.materials.append(mat)
    for i,poly in enumerate(mesh.polygons):
        poly.material_index=finishes[i];poly.use_smooth=smooth[i]
    mesh.calc_loop_triangles()
    return obj,len(mesh.loop_triangles)

if len(args)>1:
    detailed,_=make('Lantern master',72,12,True)
    bpy.ops.wm.save_as_mainfile(filepath=str(Path(args[1]).resolve()))
    bpy.data.objects.remove(detailed,do_unlink=True)
for rank,(steps,tube,limit) in enumerate([(36,6,5500),(20,4,2300),(12,3,1300)]):
    obj,count=make('Lantern_LOD'+str(rank),steps,tube)
    assert count<=limit,(rank,count,limit)
    print(f'PENDANT LOD{rank} {count} triangles',flush=True)
bpy.ops.export_scene.fbx(filepath=str(output),object_types={'MESH'},axis_forward='-Z',axis_up='Y',
                         add_leaf_bones=False,bake_anim=False,apply_unit_scale=True,mesh_smooth_type='FACE')
