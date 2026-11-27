"""Build the Harbour service machines. Run Blender -b -P build.py -- [--only name] [--review directory]."""
from argparse import ArgumentParser
from math import cos, pi, radians, sin
from pathlib import Path
import json, sys

import bpy
from bpy_extras.anim_utils import animdata_get_channelbag_for_assigned_slot
from mathutils import Vector, Quaternion
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
from porter import porter
from clerk import clerk
from distance import distant
from utilities import sweeper, runner
from led import FAMILY
BUILDERS={'Porter':porter,'Clerk':clerk,'Sweeper':sweeper,'Runner':runner}
ROOT=HERE.parents[2]
ART=HERE.parent/'art/robots/harbour'
RUNTIME=ROOT/'unity/Assets/Suborned/Scenery/HarbourRobots'


def image_node(tree, file, linear=False):
    image=bpy.data.images.load(str(RUNTIME/file),check_existing=True)
    if linear:
        image.colorspace_settings.name='Non-Color'
        image.alpha_mode='CHANNEL_PACKED'
    node=tree.nodes.new('ShaderNodeTexImage');node.image=image
    return node


def materials(tool):
    body=bpy.data.materials.new('HarbourServiceFinish');body.use_nodes=True
    tree=body.node_tree;bs=tree.nodes.get('Principled BSDF')
    paint=image_node(tree,'ServiceColour.png')
    response=image_node(tree,'ServiceSurface.png',True); split=tree.nodes.new('ShaderNodeSeparateColor')
    tree.links.new(paint.outputs['Color'],bs.inputs['Base Color']);tree.links.new(response.outputs['Color'],split.inputs['Color'])
    tree.links.new(split.outputs['Red'],bs.inputs['Metallic']);tree.links.new(split.outputs['Green'],bs.inputs['Coat Weight'])
    inv=tree.nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
    tree.links.new(response.outputs['Alpha'],inv.inputs[1]);tree.links.new(inv.outputs[0],bs.inputs['Roughness'])
    family=FAMILY[tool]
    display=bpy.data.materials.new('Harbour'+family+'Display');display.use_nodes=True
    tree=display.node_tree;bs=tree.nodes.get('Principled BSDF');img=image_node(tree,family+'Display.png')
    uv=tree.nodes.new('ShaderNodeTexCoord');map_node=tree.nodes.new('ShaderNodeMapping');map_node.name='LED register'
    map_node.inputs['Scale'].default_value=(.25,.5,1)
    map_node.inputs['Location'].default_value=(0,.5,0)
    tree.links.new(uv.outputs['UV'],map_node.inputs['Vector']);tree.links.new(map_node.outputs['Vector'],img.inputs['Vector'])
    tree.links.new(img.outputs['Color'],bs.inputs['Base Color']);tree.links.new(img.outputs['Color'],bs.inputs['Emission Color'])
    bs.inputs['Emission Strength'].default_value=2.3;bs.inputs['Roughness'].default_value=.34
    bs.inputs['Specular IOR Level'].default_value=.18
    return body,display


def motion(tool):
    objects=bpy.data.objects; scene=bpy.context.scene; scene.frame_start=1;scene.frame_end=121;scene.render.fps=24
    def rotate(name, frame, x=0,y=0,z=0):
        obj=objects[name];obj.rotation_mode='XYZ';obj.rotation_euler=tuple(radians(v) for v in (x,y,z))
        obj.keyframe_insert('rotation_euler',frame=frame,group='Mechanism')
    if tool=='Porter':
        deck=objects['Porter_Deck'];rest=deck.location.copy()
        # The whole deck translates by the motion of the lower pin, so its top remains level.
        arm=Vector((0,-.595,-.142))*1.05
        for f in range(1,122):
            rise=min(1,max(0,(f-20)/29));lower=min(1,max(0,(f-75)/28))
            a=-21*(cos(pi*lower)-cos(pi*rise))/2
            for name in ['Porter_LinkUpper','Porter_LinkLower']:rotate(name,f,x=a)
            deck.location=rest+Quaternion((1,0,0),radians(a))@arm-arm
            deck.keyframe_insert('location',frame=f,group='Level luggage lift')
        for w in ['Porter_WheelL','Porter_WheelR','Porter_NoseWheel']:
            for f,a in [(1,0),(20,80),(103,80),(121,160)]:rotate(w,f,x=a*(3.15 if w.endswith('NoseWheel') else 1))
        for f,a in ((1,-7),(43,7),(82,3),(121,-7)):rotate('Porter_Head',f,z=a)
    elif tool=='Clerk':
        for f,t in [(1,0),(26,0),(54,1),(76,1),(105,0),(121,0)]:
            rotate('Clerk_PickUpper',f,x=-14*t,y=-9*t)
            rotate('Clerk_PickFore',f,x=-23*t,y=5*t)
            rotate('Clerk_PickHand',f,x=18*t,z=-13*t)
            rotate('Clerk_BowlFore',f,x=-4*t)
            rotate('Clerk_Head',f,x=4*(1-t),z=-9+16*t)
        for i in range(3):
            for f,a in ((1,0),(26,145),(105,145),(121,240)):rotate('Clerk_Caster'+str(i),f,x=a)
        objects['Clerk_Papers'].hide_render=True;objects['Clerk_Papers'].hide_set(True)
    elif tool=='Sweeper':
        for f in range(1,122,6):
            for name,sign in [('L',1),('R',-1)]:
                rotate('Sweeper_Brush'+name,f,z=sign*(f-1)*15)
                rotate('Sweeper_Wheel'+name,f,x=(f-1)*3)
    else:
        for f,t in [(1,0),(22,0),(50,1),(74,1),(101,0),(121,0)]:
            rotate('Runner_Flag',f,x=-59*(1-t))
            rotate('Runner_Hatch',f,x=-40*t)
            rotate('Runner_Head',f,z=-8*t)
        for w in ['Runner_WheelL','Runner_WheelR','Runner_Caster']:
            for f,a in [(1,0),(22,160),(101,160),(121,360)]:rotate(w,f,x=a*(.15/.067 if w.endswith('Caster') else 1))
    tree=bpy.data.materials['Harbour'+FAMILY[tool]+'Display'].node_tree
    loc=tree.nodes['LED register'].inputs['Location']
    for f,k in ((1,0),(24,1),(25,2),(27,1),(28,0),(48,3),(73,0),(94,1),(95,2),(97,0),(121,0)):
        loc.default_value=(k%4*.25,(1-k//4)*.5,0);loc.keyframe_insert('default_value',frame=f)
    for curve in animdata_get_channelbag_for_assigned_slot(tree.animation_data).fcurves:
        for key in curve.keyframe_points:key.interpolation='CONSTANT'
    for frame in range(1,122):
        scene.frame_set(frame)
        x,y,_=loc.default_value
        assert abs(x*4-round(x*4))<.0001 and abs(y*2-round(y*2))<.0001
    scene.frame_set(1)


def studio(tool):
    s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=True
    try:
        prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='METAL';prefs.get_devices()
        for dev in prefs.devices:dev.use=dev.type=='METAL'
        if any(d.use for d in prefs.devices):s.cycles.device='GPU'
    except (TypeError,RuntimeError):pass
    s.world=bpy.data.worlds.new('Pearl studio');s.world.use_nodes=True
    wn=s.world.node_tree;bg=wn.nodes.get('Background');bg.inputs[1].default_value=.23
    hdr=Path(bpy.utils.system_resource('DATAFILES'))/'studiolights/world/studio.exr'
    if hdr.exists():
        light=wn.nodes.new('ShaderNodeTexEnvironment');light.image=bpy.data.images.load(str(hdr));wn.links.new(light.outputs['Color'],bg.inputs[0])
    else:bg.inputs[0].default_value=(.72,.75,.72,1)
    def aim(obj,at):obj.rotation_euler=(Vector(at)-obj.location).to_track_quat('-Z','Y').to_euler()
    target=(0,-.04,{'Porter':.70,'Clerk':.81,'Sweeper':.24,'Runner':.40}[tool])
    def lamp(name,pos,size,power,colour,sy):
        light=bpy.data.lights.new(name,'AREA');light.energy=power;light.shape='RECTANGLE';light.size=size;light.size_y=sy;light.color=colour
        o=bpy.data.objects.new(name,light);s.collection.objects.link(o);o.location=pos;aim(o,target)
    lamp('Long softbox',(-3,-3,4.8),3.8,390,(1,.86,.71),2.3)
    lamp('Side reflection',(3.4,-.3,3.2),1.7,330,(.8,.91,1),3.2)
    lamp('Crown reflection',(-.8,3.3,3.4),2.6,480,(1,.88,.69),1.5)
    bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Studio floor'
    mat=bpy.data.materials.new('Studio ivory');mat.diffuse_color=(.33,.27,.19,1);floor.data.materials.append(mat)
    cam=bpy.data.objects.new('Product camera',bpy.data.cameras.new('Product camera'));s.collection.objects.link(cam);s.camera=cam
    cam.location={'Porter':(2.7,-3.3,1.82),'Clerk':(2.55,-3.5,1.77),
                  'Sweeper':(1.6,-2.32,.99),'Runner':(1.36,-2.60,1.02)}[tool];aim(cam,target)
    cam.data.type='ORTHO';cam.data.ortho_scale={'Porter':2.64,'Clerk':2.26,'Sweeper':1.04,'Runner':1.05}[tool]
    s.render.resolution_x=900;s.render.resolution_y=900;s.render.resolution_percentage=100
    s.render.image_settings.file_format='PNG';s.view_settings.view_transform='AgX';s.view_settings.look='AgX - Medium High Contrast'
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                area.spaces.active.region_3d.view_perspective='CAMERA'
                area.spaces.active.shading.type='MATERIAL'
                area.spaces.active.overlay.show_overlays=False
    return s


def count(objects):
    total=0
    for obj in objects:
        mesh=obj.data;mesh.calc_loop_triangles();total+=len(mesh.loop_triangles)
    return total


def audit_objects(objects):
    for o in objects:
        assert o.data.uv_layers and len(o.data.uv_layers[0].data)==len(o.data.loops),o.name
        assert all(all(abs(v)<20 for v in pos.co) for pos in o.data.vertices),o.name
    bpy.context.view_layer.update()
    corners=[o.matrix_world@Vector(corner) for o in objects for corner in o.bound_box]
    bounds=[[round(fn(v[k] for v in corners),4) for k in range(3)] for fn in (min,max)]
    return {'triangles':count(objects),'parts':len(objects),'bounds':bounds}


def check_motion(tool):
    scene=bpy.context.scene;objects=bpy.data.objects
    scene.frame_set(1)
    if tool=='Porter':
        deck=objects['Porter_Deck']
        rest=deck.matrix_world.copy()
        pins=[]
        for name,height in [('Lower',-.077),('Upper',.058)]:
            link=objects['Porter_Link'+name]
            on_deck=Vector((0,0,height*1.05))
            pins.append((link,link.matrix_world.inverted()@(rest@on_deck),on_deck))
        stroke=0
        for frame in range(1,122):
            scene.frame_set(frame);pose=deck.matrix_world
            assert pose.to_quaternion().rotation_difference(rest.to_quaternion()).angle<.0001
            assert all(((link.matrix_world@hinge)-(pose@offset)).length<.0001 for link,hinge,offset in pins)
            stroke=max(stroke,pose.translation.z-rest.translation.z)
        assert .23<stroke<.24,stroke
    elif tool=='Clerk':
        hand=objects['Clerk_PickHand'];rest=hand.matrix_world.translation.copy()
        scene.frame_set(60)
        assert .17<(hand.matrix_world.translation-rest).length<.2
        assert objects['Clerk_Stem'].parent==hand and objects['Clerk_Bowl'].parent==objects['Clerk_BowlHand']
    elif tool=='Sweeper':
        scene.frame_set(19)
        left=objects['Sweeper_BrushL'].rotation_euler.z;right=objects['Sweeper_BrushR'].rotation_euler.z
        assert left>4 and abs(left+right)<.0001
    else:
        assert objects['Runner_Flag'].rotation_euler.x<-.9
        scene.frame_set(60)
        assert abs(objects['Runner_Flag'].rotation_euler.x)<.001 and objects['Runner_Hatch'].rotation_euler.x<-.65
    scene.frame_set(1)


def make(tool, builder, review):
    stats={}
    for level,label in enumerate(('source','L0','L1','L2')):
        bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0
        utility=tool in ('Sweeper','Runner')
        body,display=materials(tool);model=distant(tool) if level==3 and not utility else builder(level);objects=model.build(body,display)
        stats[label]=audit_objects(objects)
        if level==0:
            motion(tool);check_motion(tool);s=studio(tool);bpy.ops.file.pack_all()
            bpy.ops.object.select_all(action='DESELECT');objects[0].select_set(True);bpy.context.view_layer.objects.active=objects[0]
            bpy.ops.wm.save_as_mainfile(filepath=str(ART/(tool+'.blend')),compress=True)
            if review:
                for frame in (1,60):
                    s.frame_set(frame);s.render.filepath=str(review/(tool+'-'+str(frame)+'.png'));bpy.ops.render.render(write_still=True)
                clay=bpy.data.materials.new('Shape study');clay.diffuse_color=(.44,.43,.39,1)
                for obj in objects:obj.data.materials[0]=clay
                s.frame_set(1);s.render.filepath=str(review/(tool+'-clay.png'));bpy.ops.render.render(write_still=True)
        else:
            name=tool+'-'+label
            bpy.ops.object.select_all(action='DESELECT')
            for obj in objects:obj.select_set(True)
            bpy.context.view_layer.objects.active=objects[0]
            bpy.ops.export_scene.fbx(filepath=str(RUNTIME/(name+'.fbx')),use_selection=True,object_types={'MESH'},
                                     axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
                                     bake_anim=False,add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='STRIP')
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.fbx(filepath=str(RUNTIME/(name+'.fbx')),use_anim=False)
            imported=[o for o in bpy.context.scene.objects if o.type=='MESH'];a=audit_objects(imported)
            assert a['triangles']==stats[label]['triangles'] and a['parts']==len(objects),(tool,label,a,stats[label])
            ceiling=(6500,2300,650) if utility else (11000,4000,800)
            assert a['triangles'] <= ceiling[level-1],(tool,label,a)
            assert {m.name for o in imported for m in o.data.materials}=={'HarbourServiceFinish','Harbour'+FAMILY[tool]+'Display'}
            # Keep the floor and silhouette at the same world scale at every transition.
            assert all(abs(v-w)<.04 for corner,base in zip(a['bounds'],stats['source']['bounds']) for v,w in zip(corner,base))
    print('HARBOUR AUDIT',tool,json.dumps(stats),flush=True)
    return stats


if __name__=='__main__':
    parser=ArgumentParser();parser.add_argument('--only',choices=BUILDERS);parser.add_argument('--review',type=Path)
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    if args.review:args.review.mkdir(exist_ok=True,parents=True)
    for tool,builder in BUILDERS.items():
        if args.only is None or args.only==tool:make(tool,builder,args.review)
