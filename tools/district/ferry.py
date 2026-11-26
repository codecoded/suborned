"""Blender source for the Harbour Ferries pavilion. Export paths are directory and working .blend."""
from math import atan2, ceil, cos, pi, sin
from pathlib import Path
import random
import sys
import bpy
from mathutils import Vector

out, master = map(lambda s: Path(s).resolve(), sys.argv[sys.argv.index('--')+1:])
bpy.ops.wm.read_factory_settings(use_empty=True)
TAU = 2*pi
stone, soffit, teal, glass, glass2, lobby, brass, oxide, shade, leaf, tips, bark, opal, patina, dial, letters = range(16)
material = bpy.data.materials.new('Harbour trim sheet')
material.use_nodes = True
tex = material.node_tree.nodes.new('ShaderNodeTexImage')
tex.image = bpy.data.images.load(str(out.parent/'Materials/Play/FerryColour.png'))
material.node_tree.links.new(tex.outputs['Color'], material.node_tree.nodes['Principled BSDF'].inputs['Base Color'])

class Model:
    def __init__(self, name, rank):
        self.name, self.rank = name, rank
        self.verts, self.indices, self.faces, self.uv, self.smooth = [], {}, [], [], []
        self.count = (12,7,3)[rank]
        self.rng = random.Random(116)

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

    def windows(self,x,y,w,d,r,h0,h1):
        span=(1.85,2.9,5.2)[self.rank]
        loop=self.contour(x,y,w,d,r,(8,5,3)[self.rank],span)
        for i,(a,b) in enumerate(zip(loop,loop[1:]+loop[:1])):
            u,v=a; ex,ny=b
            slot=[glass,glass2,glass,lobby,glass2,glass2,lobby][i%7]
            self.face([(u,v,h0),(ex,ny,h0),(ex,ny,h1),(u,v,h1)],slot)
            if self.rank==0 and i%2==0:
                self.beam((u,v,h0),(u,v,h1),.067,brass,4)
        self.shape(x,y,w+.14,d+.14,r+.075,h0+.34*(h1-h0),h0+.34*(h1-h0)+.085,teal)

    def cornice(self,x,y,w,d,r,h):
        self.tiers(x,y,[(h-.38,w-.7,d-.7,r-.3,soffit),(h-.23,w,d,r,soffit),
                        (h-.13,w+.10,d+.10,r+.04,brass),(h+.17,w,d,r,stone),
                        (h+.30,w-.18,d-.18,r-.09,stone)])
        if self.rank<2: self.shape(x,y,w+.025,d+.025,r,h+.018,h+.073,patina)

    def bush(self,x,y,z,rx,ry,height,phase):
        n=(9,7,5)[self.rank]; rings=[]
        for level,scale in [(0,.64),(.24,1),(.66,.87),(.93,.44)]:
            row=[]
            for i in range(n):
                a=TAU*i/n+phase+level*.25
                rr=scale*(1+.12*sin(3*a+phase))
                row.append((x+rx*rr*cos(a),y+ry*rr*sin(a),z+height*(level+.08*sin(a*4+phase)*level)))
            if rings:
                prev=rings[-1]
                for i in range(n):
                    j=(i+1)%n
                    slot=shade if level<.3 else (leaf if (i+int(phase*3))%4!=1 else tips)
                    self.face([prev[i],prev[j],row[j],row[i]],slot)
            rings.append(row)
        self.face(rings[-1],leaf)

    def palm(self,x,y,z,height,heading):
        r=self.rank
        sway=height*.10
        joints=[(x+sway*t*t*cos(heading),y+sway*t*t*sin(heading),z+height*t) for t in (0,.36,.70,1)]
        for i in range(3):
            self.beam(joints[i],joints[i+1],height*(.028-i*.004),bark,(9,6,5)[r],height*(.024-i*.004))
        crown=Vector(joints[-1])
        for j in range((13,10,8)[r]):
            a=heading+2.39996*j; extent=height*(.50+.07*sin(j*1.6)); rise=height*(.28+.09*cos(j))
            rad=Vector((cos(a),sin(a),0)); cross=Vector((-sin(a),cos(a),0))
            prev=None
            steps=(8,5,4)[r]
            for k in range(steps+1):
                t=k/steps; h=rise*sin(pi*t*.95)-height*.29*t*t
                spine=crown+rad*(extent*t)+Vector((0,0,h))
                width=max(.009,height*(.062 if r<2 else .085)*sin(pi*t)**.65)
                edge=spine-Vector((0,0,width*.40))
                new=[edge-cross*width,spine,edge+cross*width]
                if prev:
                    for b in range(2):
                        strip=[prev[b],prev[b+1],new[b+1],new[b]]
                        self.face(strip,leaf if (b+j)%3 else tips);self.face(strip[::-1],shade)
                    if r==0 and k not in (1,steps):
                        for sign in (-1,1):
                            stem=spine-rad*.26
                            blade=[stem,edge+cross*sign*width*.80,edge+rad*.38+cross*sign*width*1.75-Vector((0,0,.19))]
                            self.face(blade,leaf if j%3 else tips);self.face(blade[::-1],shade)
                prev=new

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
    s.tiers(0,0,[(-2.2,23.9,44,7,teal),(-.22,24.2,44.6,7.2,teal),(.16,25.1,45.2,7.3,soffit),
                 (.50,25.1,45.2,7.3,stone),(.62,24.6,44.7,7.1,brass)])
    s.shape(.30,0,21.9,41.0,6.7,.56,1.35,stone)
    s.windows(.30,0,21.7,40.9,6.7,1.34,4.33)
    s.cornice(0,0,25.5,45.9,7.6,4.61)
    s.shape(.35,0,21.7,40.85,6.7,4.68,5.10,oxide)
    s.shape(.8,2,17.8,32.2,6.1,4.92,5.67,stone)
    s.windows(.8,2,17.65,32.1,6.1,5.65,8.60)
    s.cornice(.8,2,20.6,35.3,7.25,8.90)
    s.shape(.8,2,17.55,31.7,6.1,9.04,9.60,teal)
    # The upper salon has its own belvedere and a patinated, ribbed shallow dome.
    s.shape(-.5,6.4,11.6,16.8,5.5,9.23,9.90,stone)
    s.windows(-.5,6.4,11.45,16.6,5.45,9.87,12.38)
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
    s.plate(-10.61,0,2.91,8.48,1.32,pi,letters,section=(0,.51,1,1))
    s.plate(.30,-20.51,2.95,7.95,1.24,-pi/2,letters,section=(0,.51,1,1))
    # The rail and ferry booking window opens onto the Skyport apron, beneath the west canopy.
    s.shape(-10.64,-7.65,.37,5.9,.15,1.02,2.96,teal)
    s.shape(-10.90,-7.65,.67,6.06,.25,1.04,1.24,brass)
    s.plate(-10.84,-7.65,2.04,5.48,1.39,pi,letters,section=(0,0,1,.49))
    # A shaded waiting walk follows the public edge; all supports stand seaward of the walk mesh.
    for n in (-11.6,-5.8,5.8,11.6):
        s.beam((-11.88,n,.61),(-12.20,n,4.42),.20,stone,(9,6,4)[s.rank],.14)
    for xx in (-1,1):
        for j,n in enumerate((-10.3,-4.8,.7,6.0,11.2)):
            s.bush(xx*10.6,n,4.86,1.28,3.19,.99+.25*cos(j),j*.82+xx)
    for j,e in enumerate((-5.8,-2.1,1.6,5.15)):
        s.bush(e,-17.4,4.87,2.15,1.49,1.1,j)
    for xx in (-1,1):
        for j,n in enumerate((-6.1,-2.1,2.7,7.0)):
            s.bush(.8+xx*7.73,n,9.2,1.72,2.47,.88,j+xx)
    for i,(e,n,h,size,rot) in enumerate([(-8.4,-12.8,4.90,5.8,.4),(7.75,-13.6,4.90,6.5,1.5),
                                          (-9.1,10.8,4.90,6.2,2.1),(6.4,-7.9,9.2,5.0,.3),(-3.8,-9.1,9.2,4.7,1.7)]):
        s.palm(e,n,h,size,rot)

def build(name, func, budgets):
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
build('FerryPavilion',ferry,(16000,8000,4400))
bpy.ops.wm.save_as_mainfile(filepath=str(master))
