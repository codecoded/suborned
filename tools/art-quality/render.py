"""Render pilot candidates on the SAME installed meshes, isolated Blender process.
Blender -b -t 2 -P tools/art-quality/render.py -- out/scratch/codex/material-pilot
These are material comparisons, not claims of Unity rendering parity.
"""
import hashlib
import json
import math
import re
import sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
M=json.loads((OUT/'manifest.json').read_text())
UA=ROOT/'unity/Assets/Suborned'
index={}
for root in [UA/'Kit',UA/'People',OUT]:
    for p in root.rglob('*.meta'):
        match=re.search(r'^guid: (\w+)',p.read_text(errors='ignore'),re.M)
        if match:index[match[1]]=p.with_suffix('')
variants={Path(x['source']).stem:Path(x['candidate']) for x in M['materials']}

def num(t,key,default):
    m=re.search(r'- '+key+r': ([\d.eE+-]+)',t);return float(m[1]) if m else default

def lin(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4

def material(p):
    t=p.read_text();mat=bpy.data.materials.new(p.stem);mat.use_nodes=True
    nt=mat.node_tree;b=nt.nodes['Principled BSDF'];uv=nt.nodes.new('ShaderNodeTexCoord')
    c=re.search(r'- _BaseColor: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)',t)
    color=[float(x) for x in c.groups()] if c else [1,1,1,1]
    const=nt.nodes.new('ShaderNodeRGB');const.outputs[0].default_value=(*[lin(x) for x in color[:3]],1)
    base=const.outputs[0]
    def texture(slot,noncolor=False):
        m=re.search(r'- '+slot+r':\n\s*m_Texture: \{fileID: [^,}]+, guid: (\w+)[^\n]*\n\s*m_Scale: \{x: ([^,]+), y: ([^}]+)',t)
        if not m:return None
        path=index.get(m[1])
        if not path or not path.exists():raise FileNotFoundError((p,slot,path))
        tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True)
        if noncolor:tex.image.colorspace_settings.name='Non-Color'
        mapping=nt.nodes.new('ShaderNodeMapping');mapping.inputs['Scale'].default_value=(float(m[2]),float(m[3]),1)
        nt.links.new(uv.outputs['UV'],mapping.inputs['Vector']);nt.links.new(mapping.outputs['Vector'],tex.inputs['Vector'])
        return tex
    def mul(a,b):
        n=nt.nodes.new('ShaderNodeMixRGB');n.blend_type='MULTIPLY';n.inputs[0].default_value=1
        nt.links.new(a,n.inputs[1]);nt.links.new(b,n.inputs[2]);return n.outputs[0]
    tx=texture('_BaseMap')
    if tx:base=mul(base,tx.outputs['Color'])
    if '_DETAIL_MULX2' in t:
        det=texture('_DetailAlbedoMap',True)
        if det:
            scale=nt.nodes.new('ShaderNodeVectorMath');scale.operation='SCALE';scale.inputs['Scale'].default_value=2
            nt.links.new(det.outputs['Color'],scale.inputs[0]);base=mul(base,scale.outputs['Vector'])
    mask=texture('_MetallicGlossMap',True) if '_METALLICSPECGLOSSMAP' in t else None
    b.inputs['Metallic'].default_value=num(t,'_Metallic',0);b.inputs['Roughness'].default_value=1-num(t,'_Smoothness',.15)
    if mask:
        sep=nt.nodes.new('ShaderNodeSeparateColor');nt.links.new(mask.outputs['Color'],sep.inputs[0]);nt.links.new(sep.outputs['Red'],b.inputs['Metallic'])
        scale=nt.nodes.new('ShaderNodeMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=num(t,'_Smoothness',1)
        nt.links.new(mask.outputs['Alpha'],scale.inputs[0]);inv=nt.nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
        nt.links.new(scale.outputs[0],inv.inputs[1]);nt.links.new(inv.outputs[0],b.inputs['Roughness'])
        # glTF/URP AO attenuates indirect only. This preview deliberately does
        # not multiply it into albedo (which would darken direct sun incorrectly).
    nt.links.new(base,b.inputs['Base Color'])
    normal=texture('_BumpMap',True) if '_NORMALMAP' in t else None
    detail=texture('_DetailNormalMap',True) if '_DETAIL_MULX2' in t else None
    if normal or detail:
        # Pilot targets have either base or detail normals, not both.
        tex=normal or detail;n=nt.nodes.new('ShaderNodeNormalMap')
        n.inputs['Strength'].default_value=num(t,'_BumpScale',1) if normal else num(t,'_DetailNormalMapScale',1)
        nt.links.new(tex.outputs['Color'],n.inputs['Color']);nt.links.new(n.outputs['Normal'],b.inputs['Normal'])
    if num(t,'_ClearCoatMask',0)>0:
        b.inputs['Coat Weight'].default_value=num(t,'_ClearCoatMask',0);b.inputs['Coat Roughness'].default_value=1-num(t,'_ClearCoatSmoothness',.9)
    if color[3]<1:
        b.inputs['Alpha'].default_value=color[3];mat.surface_render_method='DITHERED'
    return mat

assets=[('cafe',UA/'Kit/Models/Plaza/DK_CafeBay_v2.fbx',None),('planter',UA/'Kit/Models/Plaza/DK_PlanterRun_4_A.fbx',None),('scout',UA/'People/Figures/scout/scout.fbx','LOD2')]
evidence=[]
for key,fbx,lod in assets:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc=bpy.context.scene;sc.render.engine='BLENDER_EEVEE';sc.eevee.taa_render_samples=32
    sc.render.resolution_x=900;sc.render.resolution_y=650;sc.render.resolution_percentage=100
    sc.view_settings.view_transform='AgX'
    bpy.ops.import_scene.fbx(filepath=str(fbx));objects=[o for o in sc.objects if o.type=='MESH']
    for o in objects:
        if lod and lod not in o.name:o.hide_render=True
    meshes=[o for o in objects if not o.hide_render];original={}
    for o in meshes:
        refs=[]
        for slot in o.material_slots:
            name=re.sub(r'\.\d{3}$','',slot.material.name)
            if key=='scout':name='scout_body_512'
            p=(UA/'People/Figures/scout/Materials' if key=='scout' else UA/'Kit/Materials')/(name+'.mat')
            if not p.exists():raise FileNotFoundError(p)
            refs.append(p)
        original[o.name]=refs
    points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    low=Vector(tuple(min(v[i] for v in points) for i in range(3)));high=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=(low+high)/2;span=max(high-low)
    bpy.ops.mesh.primitive_plane_add(size=200,location=(center.x,center.y,low.z-.015));floor=bpy.context.object
    floor.data.materials.append(material(UA/'Kit/Materials/DK_Limestone.mat'))
    world=bpy.data.worlds.new('studio');world.use_nodes=True;sc.world=world
    sun=bpy.data.lights.new('key','SUN');sun.angle=.12;so=bpy.data.objects.new('key',sun);sc.collection.objects.link(so);so.rotation_euler=(math.radians(35),math.radians(-20),math.radians(-30))
    fill=bpy.data.lights.new('fill','AREA');fill.energy=650;fill.shape='DISK';fill.size=5
    fo=bpy.data.objects.new('fill',fill);sc.collection.objects.link(fo);fo.location=center+Vector((-4,-6,4));fo.rotation_euler=(center-fo.location).to_track_quat('-Z','Y').to_euler()
    cam=bpy.data.cameras.new('compare');co=bpy.data.objects.new('compare',cam);sc.collection.objects.link(co);sc.camera=co;cam.type='ORTHO'
    for phase in ['day','night']:
        sun.energy=2 if phase=='day' else .15;sun.color=(1,.89,.75) if phase=='day' else (.5,.65,1)
        world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.65,.8,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45 if phase=='day' else .08
        fill.energy=650 if phase=='day' else 110;fill.color=(1,1,1) if phase=='day' else (1,.65,.32)
        for view in ['close','tactical']:
            if phase=='night' and view=='tactical':continue
            distance=max(span*2,8);co.location=center+Vector((.4,-1,.7)).normalized()*distance
            co.rotation_euler=(center-co.location).to_track_quat('-Z','Y').to_euler();cam.ortho_scale=span*1.35 if view=='close' else 28*900/650
            for variant in ['before','candidate']:
                cache={}
                for o in meshes:
                    for slot,p in zip(o.material_slots,original[o.name]):
                        chosen=variants.get(p.stem,p) if variant=='candidate' else p
                        if chosen not in cache:cache[chosen]=material(chosen)
                        slot.material=cache[chosen]
                path=OUT/f'{key}-{phase}-{view}-{variant}.png';sc.render.filepath=str(path);bpy.ops.render.render(write_still=True)
                evidence.append({'asset':key,'phase':phase,'view':view,'variant':variant,'image':str(path),'fbx':str(fbx),'meshSha256':hashlib.sha256(fbx.read_bytes()).hexdigest()})
    print('[pilot]',key,'done',flush=True)
(OUT/'renders.json').write_text(json.dumps(evidence,indent=2)+'\n')
