"""Blender source for the Harbour Ferries pavilion. Export paths are directory and working .blend."""
from math import ceil, cos, pi, sin
from pathlib import Path
import sys
import bpy
from mathutils import Vector

sys.path.insert(0,str(Path(__file__).resolve().parent))
from ferry_front import (BRONZE as brass, CLOCK as dial, SOFFIT as soffit, STONE as stone,
                        TILE as teal, ZINC as patina, boarding_canopy, office, window_band)

TAU = 2*pi
def use_sheet(path):
    global material
    bpy.ops.wm.read_factory_settings(use_empty=True)
    material = bpy.data.materials.new('Harbour architectural finishes')
    material.use_nodes = True
    tex = material.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(str(path))
    material.node_tree.links.new(tex.outputs['Color'], material.node_tree.nodes['Principled BSDF'].inputs['Base Color'])


class Model:
    def __init__(self, name, rank):
        self.name, self.rank = name, rank
        self.verts, self.indices, self.faces, self.uv, self.smooth = [], {}, [], [], []
        self.count = (12,7,3)[rank]

    def face(self, points, slot, uv=None, smooth=False):
        if uv is None:
            uv = [(0,0),(1,0),(1,1),(0,1)] if len(points)==4 else [
                (.5+.47*cos(TAU*i/len(points)),.5+.47*sin(TAU*i/len(points))) for i in range(len(points))]
        verts = []
        for point in points:
            key = tuple(round(k,5) for k in point)
            idx = self.indices.get(key)
            if idx is None:
                idx = len(self.verts); self.verts.append(key); self.indices[key] = idx
            verts.append(idx)
        if len(set(verts)) < 3: return
        self.faces.append(verts); self.smooth.append(smooth)
        self.uv.extend([((slot%4+.035+.93*u)/4, (3-slot//4+.035+.93*v)/4) for u,v in uv])

    @staticmethod
    def contour(x,y,w,d,r,n,span=0):
        r = min(r, min(w,d)*.499)
        points=[]
        for cx,cy,a in [(w/2-r,-d/2+r,-pi/2),(w/2-r,d/2-r,0),(-w/2+r,d/2-r,pi/2),(-w/2+r,-d/2+r,pi)]:
            points.extend([(x+cx+r*cos(a+pi*k/(2*n)),y+cy+r*sin(a+pi*k/(2*n))) for k in range(n+1)])
        if not span: return points
        divided=[]
        for a,b in zip(points,points[1:]+points[:1]):
            cuts=max(1,ceil((Vector(a)-Vector(b)).length/span))
            divided.extend([(a[0]+(b[0]-a[0])*k/cuts,a[1]+(b[1]-a[1])*k/cuts) for k in range(cuts)])
        return divided

    def tiers(self,x,y,levels):
        rings=[]
        for h,w,d,r,slot in levels:
            loop=[(e,n,h) for e,n in self.contour(x,y,w,d,r,self.count)]
            if rings:
                last=rings[-1]
                for i in range(len(loop)):
                    j=(i+1)%len(loop)
                    self.face([last[i],last[j],loop[j],loop[i]],slot)
            rings.append(loop)
        self.face(rings[0][::-1],soffit)
        self.face(rings[-1],levels[-1][-1])

    def shape(self,x,y,w,d,r,h0,h1,slot):
        self.tiers(x,y,[(h0,w,d,r,slot),(h1,w,d,r,slot)])

    def beam(self, a, b, radius, slot, sides=6, top=None):
        a,b=Vector(a),Vector(b); v=(b-a).normalized()
        right=v.cross(Vector((0,1,0) if abs(v.y)<.9 else (1,0,0))).normalized()
        up=v.cross(right)
        ra=[a+(right*cos(TAU*k/sides)+up*sin(TAU*k/sides))*radius for k in range(sides)]
        rb=[b+(right*cos(TAU*k/sides)+up*sin(TAU*k/sides))*(top if top is not None else radius) for k in range(sides)]
        for k in range(sides):
            j=(k+1)%sides; self.face([ra[k],ra[j],rb[j],rb[k]],slot,smooth=sides>=8)
        self.face(rb,slot); self.face(ra[::-1],slot)

    def cornice(self,x,y,w,d,r,h):
        self.tiers(x,y,[(h-.28,w-.7,d-.7,r-.3,soffit),(h-.13,w,d,r,soffit),
                        (h-.084,w+.065,d+.065,r+.025,brass),(h+.17,w,d,r,stone),
                        (h+.22,w-.12,d-.12,r-.06,stone)])
        if self.rank<2: self.shape(x,y,w+.025,d+.025,r,h+.042,h+.062,patina)

    def plate(self,x,y,z,width,height,heading,slot,round_face=False,section=(0,0,1,1)):
        n=Vector((cos(heading),sin(heading),0)); h=Vector((-sin(heading),cos(heading),0)); v=Vector((0,0,1)); c=Vector((x,y,z))
        if round_face:
            rim=[(.5+.50*cos(TAU*i/32),.5+.50*sin(TAU*i/32)) for i in range(32)]
        else: rim=[(0,0),(1,0),(1,1),(0,1)]
        u0,v0,u1,v1=section
        self.face([c+h*(u-.5)*width+v*(t-.5)*height for u,t in rim],slot,
                  [(u0+u*(u1-u0),v0+t*(v1-v0)) for u,t in rim])

    def mesh(self):
        mesh=bpy.data.meshes.new(self.name);mesh.from_pydata(self.verts,[],self.faces);mesh.update()
        mesh.materials.append(material)
        uv=mesh.uv_layers.new(name='Harbour atlas'); uv.uv.foreach_set('vector',[v for pair in self.uv for v in pair])
        mesh.polygons.foreach_set('use_smooth',self.smooth)
        obj=bpy.data.objects.new(self.name,mesh);bpy.context.collection.objects.link(obj)
        mesh.calc_loop_triangles()
        return obj,len(mesh.loop_triangles)

def ferry(s):
    # West is the public quay. The sea-level caisson slides beneath its walking edge.
    s.tiers(0,0,[(-2.2,23.9,44,7,patina),(-.26,24.4,44.6,7.2,soffit),(-.07,25.13,45.25,7.3,soffit),
                 (.10,25.13,45.25,7.3,stone),(.14,25.05,45.17,7.24,stone)])
    s.shape(.30,0,21.9,41.0,6.7,.13,.66,stone)
    window_band(s,.30,0,21.7,40.9,6.7,.65,4.32,booking=True)
    s.cornice(0,0,25.45,45.9,7.6,4.57)
    s.shape(.35,0,21.7,40.85,6.7,4.74,4.98,patina)
    s.shape(.8,2,17.8,32.2,6.1,4.84,5.65,stone)
    window_band(s,.8,2,17.65,32.1,6.1,5.64,8.55)
    s.cornice(.8,2,18.75,33.2,6.53,8.77)
    s.shape(.8,2,17.55,31.7,6.1,8.95,9.38,stone)
    # The upper salon has its own belvedere and a patinated, ribbed shallow dome.
    s.shape(-.5,6.4,11.6,16.8,5.5,9.23,9.90,stone)
    window_band(s,-.5,6.4,11.45,16.6,5.45,9.87,12.38)
    s.cornice(-.5,6.4,13.3,18.3,6.4,12.56)
    s.tiers(-.5,6.4,[(12.85,10.9,15.8,5.1,brass),(13.24,10.3,15.1,5,patina),
                       (13.90,8.6,12.6,4.1,patina),(14.48,5.6,8.0,2.7,patina),(14.69,1.3,2.5,.6,brass)])
    # Twin stepped clock fins. The top sits between the straight and branching sky rail.
    for off,h,w in [(-1.20,15.8,1.4),(0,20.8,2.95),(1.32,17.9,.95)]:
        s.tiers(6.8+off,7.7,[(7.6,w+1.0,4.3,.70,stone),(h-3.5,w,3.5,.52,stone),
                              (h-1.0,w*.84,2.45,.40,stone),(h,w*.58,1.34,.26,brass)])
    s.beam((6.8,7.7,20.45),(6.8,7.7,23.35),.115,brass,8,.035)
    s.shape(6.8,5.84,1.54,.25,.09,12.4,18.28,teal)
    for side in (-1,1):
        s.plate(6.8,7.7+side*1.80,16.35,2.40,2.40,side*pi/2,dial,True)
    office(s)
    boarding_canopy(s)
    # The upper lounge opens onto the seaward promenade. Thin stanchions define the roof edge.
    for side in (-1,1):
        x=side*10.43
        for y in (-10.8,-7.2,-3.6,0,3.6,7.2,10.8):
            s.beam((x,y,4.78),(x,y,5.78),.035,brass,(8,6,4)[s.rank])
        for height in (5.18,5.76):
            s.beam((x,-10.86,height),(x,10.86,height),.037,brass,(8,6,4)[s.rank])

def build(name, func, budgets, out):
    objects=[]
    for rank in range(3):
        s=Model(name+(' Close',' Street',' Horizon')[rank],rank);func(s);obj,count=s.mesh()
        print(f'{name} tier {rank}: {count} triangles',flush=True)
        assert count<=budgets[rank],(name,rank,count,budgets[rank])
        objects.append(obj)
    for axis,loc in [('East',(1,0,0)),('North',(0,1,0))]:
        obj=bpy.data.objects.new(axis,None);bpy.context.collection.objects.link(obj);obj.location=loc;objects.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',
                             bake_anim=False,add_leaf_bones=False,apply_unit_scale=True,mesh_smooth_type='FACE')
    # Keep the authored objects grouped in the source. Reuse the simple locator names in the next FBX.
    group=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(group)
    for obj in objects:
        bpy.context.collection.objects.unlink(obj);group.objects.link(obj)
        if obj.type=='EMPTY':obj.name=name+' '+obj.name
if __name__ == '__main__':
    out, master = [Path(v).resolve() for v in sys.argv[sys.argv.index('--')+1:]]
    use_sheet(out.parent/'Materials/Play/FerryColour.png')
    build('FerryPavilion',ferry,(16000,8000,4400),out)
    bpy.ops.wm.save_as_mainfile(filepath=str(master))
