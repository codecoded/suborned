"""Render helpers for the fit test (brief 069): a soft studio for close views and the play camera (ortho, 38 deg down, 38.571 px/m as the game's
play zoom). EEVEE, matte materials with the figure's own base colour."""
import bpy, math
from mathutils import Vector


def studio(sc, w, h):
    for k in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE'):
        try: sc.render.engine = k; break
        except TypeError: pass
    sc.render.resolution_x = w; sc.render.resolution_y = h; sc.render.resolution_percentage = 100
    sc.view_settings.view_transform = 'Standard'; sc.render.film_transparent = False
    w_ = bpy.data.worlds.new('w'); w_.use_nodes = True; bg = w_.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.62, 0.62, 0.64, 1); bg.inputs[1].default_value = 0.9; sc.world = w_
    sun = bpy.data.lights.new('sun', 'SUN'); sun.energy = 3.2; sun.angle = math.radians(8); so = bpy.data.objects.new('sun', sun); sc.collection.objects.link(so)
    so.rotation_euler = (math.radians(50), 0, math.radians(-30))
    bpy.ops.mesh.primitive_plane_add(size=400, location=(0, 0, 0)); g = bpy.context.active_object; g.name = 'ground'
    m = bpy.data.materials.new('ground'); m.use_nodes = True; m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.55, 0.53, 0.5, 1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 1; g.data.materials.append(m)
    try: sc.eevee.use_shadows = True
    except Exception: pass
    return g


def camera(sc, name='cam'):
    c = bpy.data.objects.new(name, bpy.data.cameras.new(name)); sc.collection.objects.link(c); sc.camera = c; return c


def look_at(cam, eye, target):
    cam.location = eye; d = Vector(target) - Vector(eye); cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()


def play_cam(cam, centre, yaw, pitch, ppm, h_px):
    cy, cp = math.radians(yaw), math.radians(pitch)
    d = Vector((math.sin(cy) * math.cos(cp), math.cos(cy) * math.cos(cp), -math.sin(cp)))
    cam.data.type = 'ORTHO'; cam.data.sensor_fit = 'VERTICAL'; cam.data.ortho_scale = h_px / ppm; cam.data.clip_end = 500
    cam.location = Vector(centre) - d * 60; cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
