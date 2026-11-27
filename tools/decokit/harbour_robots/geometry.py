"""Rigid parts in metres. The machines face Blender -Y and stand on Z=0."""
from math import cos, pi, sin
from pathlib import Path
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'district'))
from ferry import Model

TAU = 2 * pi
IVORY, CORAL, MINT, CHROME, STEEL, BRASS, TYRE, INK, LEATHER, WOOD, PETAL, LEAF, CREAM, PAPER, DARK_METAL, BLACK = range(16)


def around(n):
    return [TAU * i / n for i in range(n)]


def ellipse(cx, cy, rx, ry, n, power=1):
    def curve(v):
        return (1 if v >= 0 else -1) * abs(v) ** power
    return [(cx + rx * curve(cos(a)), cy + ry * curve(sin(a))) for a in around(n)]


class Part(Model):
    def __init__(self, owner, name, pivot, parent):
        super().__init__(name, min(max(owner.detail - 1, 0), 2))
        self.owner, self.pivot, self.parent = owner, Vector(pivot), parent

    def n(self, source, near, middle, far):
        return (source, near, middle, far)[self.owner.detail]

    def shell(self, levels, power=1, tilt=0, sides=None):
        """Stations specify height, half-width, half-depth, centre X/Y and finish up to that station."""
        sides = sides or self.n(48, 32, 18, 10)
        prev = None
        for z, rx, ry, cx, cy, finish in levels:
            loop = [(x, y, z + tilt * (y - cy)) for x,y in ellipse(cx,cy,rx,ry,sides,power)]
            if prev is None:
                self.face(loop[::-1], finish)
            else:
                self.bridge(prev, loop, finish)
            prev = loop
        self.face(prev, finish)

    def bridge(self, a, b, finish, smooth=True):
        for i in range(len(a)):
            k = (i+1) % len(a)
            self.face([a[i],a[k],b[k],b[i]],finish,smooth=smooth)

    def spindle(self, a, b, sections, sides=None):
        """Turned part about the line a–b. Each station is a fraction, radius and finish."""
        a,b = Vector(a),Vector(b)
        axis = (b-a).normalized()
        u = axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized()
        v = axis.cross(u)
        sides = sides or self.n(40,24,14,8)
        prev = None
        for t, radius, finish in sections:
            mid = a.lerp(b,t)
            ring = [mid + radius * (cos(k)*u + sin(k)*v) for k in around(sides)]
            if prev is None:
                self.face(ring[::-1],finish)
            else:
                self.bridge(prev,ring,finish)
            prev = ring
        self.face(prev,finish)

    def rod(self, a, b, r, finish, sides=None):
        self.beam(a,b,r,finish,sides or self.n(16,12,8,5))

    def pipe(self, points, radius, finish, sides=None):
        """Continuous swept tube, with no overlapping joint caps at each bend."""
        points = [Vector(x) for x in points]
        vec = (points[1]-points[0]).normalized()
        side = vec.cross(Vector((0,0,1)) if abs(vec.z)<.9 else Vector((0,1,0))).normalized()
        prev = None
        for i, p in enumerate(points):
            tangent = (points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
            side = (side-tangent*side.dot(tangent)).normalized()
            up = tangent.cross(side)
            loop = [p+radius*(cos(k)*side+sin(k)*up) for k in around(sides or self.n(12,8,6,5))]
            if prev is None:
                self.face(loop[::-1],finish)
            else:
                self.bridge(prev,loop,finish)
            prev = loop
        self.face(prev,finish)

    def plaque(self, x, y, z, width, height, radius, finish):
        poly = self.contour(0,0,width,height,radius,self.n(6,4,2,1))
        self.face([(x+u,y,z+v) for u,v in poly],finish)

    def side_casting(self, profile, widths, finish):
        """Bevel a YZ side outline, keeping the edge away from the wheel recess."""
        if sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(profile,profile[1:]+profile[:1])) < 0:
            profile=list(reversed(profile))
        my = sum(p[0] for p in profile)/len(profile)
        mz = sum(p[1] for p in profile)/len(profile)
        x0,x1 = widths
        prev = None
        bevel=min(.025,(x1-x0)*.26)
        for x,k in ((x0,.95),(x0+bevel,1),(x1-bevel,1),(x1,.95)):
            ring = [(x,my+(y-my)*k,mz+(z-mz)*k) for y,z in profile]
            if prev is None:
                self.face(ring[::-1],finish)
            else:
                self.bridge(prev,ring,finish)
            prev=ring
        self.face(prev,finish)

    def object(self, material, display):
        mesh = bpy.data.meshes.new(self.name)
        mesh.from_pydata([(Vector(v)-self.pivot)*self.owner.scale for v in self.verts],[],self.faces)
        mesh.materials.append(display if self.name.endswith('_Face') else material)
        for poly,smooth in zip(mesh.polygons,self.smooth):
            poly.use_smooth=smooth
        uv=mesh.uv_layers.new(name='Finish')
        for l, pair in zip(uv.data,self.uv):
            l.uv=pair
        if self.name.endswith('_Face'):
            # Poly coordinates on the display map the entire glass to one state in a four-state atlas.
            for poly in mesh.polygons:
                for index, pair in zip(poly.loop_indices,((0,0),(1,0),(1,1),(0,1))):
                    uv.data[index].uv=pair
        mesh.update()
        obj=bpy.data.objects.new(self.name,mesh)
        bpy.context.scene.collection.objects.link(obj)
        if self.parent:
            obj.parent=self.parent.obj
            obj.location=(self.pivot-self.parent.pivot)*self.owner.scale
        else:
            obj.location=self.pivot*self.owner.scale
        self.obj=obj
        return obj


class Machine:
    def __init__(self, detail):
        self.detail=detail
        self.scale=1.0
        self.parts=[]

    def part(self, name, pivot=(0,0,0), parent=None):
        part=Part(self,name,pivot,parent)
        self.parts.append(part)
        return part

    def build(self, material, display):
        return [p.object(material,display) for p in self.parts]


def panel_screen(head, label, y, z, w, h):
    """The inset dark glass is wider than the emissive strip. The shell masks its edges."""
    head.plaque(0,y+.002,z,w,h,h*.4,CHROME)
    head.plaque(0,y-.001,z,w-.018,h-.015,(h-.015)*.4,INK)
    face = head.owner.part(label+'_Face',head.pivot,head)
    front=y-.003
    face.face([(-w*.40,front,z-h*.31),(w*.40,front,z-h*.31),(w*.40,front,z+h*.31),(-w*.40,front,z+h*.31)],INK)
    return face


def bearing(part, x,y,z, radius, side=1):
    a=(x-.018*side,y,z); b=(x+.018*side,y,z)
    sections=[(0,radius*.76,DARK_METAL),(.12,radius,STEEL),(.64,radius,CHROME),(.75,radius*.91,BRASS),(1,radius*.67,STEEL)]
    part.spindle(a,b,sections if part.owner.detail<3 else [sections[0],sections[-1]],part.n(32,18,10,6))


def wheel(part, c, radius, width, side):
    x,y,z = c
    a=(x-side*width/2,y,z); b=(x+side*width/2,y,z)
    profile=[(0,.76,DARK_METAL),(.04,.90,TYRE),(.19,.985,TYRE),(.36,1,TYRE),(.69,1,TYRE),
             (.88,.97,TYRE),(.98,.87,TYRE),(1,.78,TYRE),(1.02,.76,CHROME),(1.04,.7,CHROME),
             (1.03,.66,INK),(1.045,.61,STEEL),(1.16,.39,STEEL),(1.19,.20,CHROME),(1.20,.12,BRASS)]
    if part.owner.detail==2:profile=[profile[i] for i in (0,2,4,7,10,12,14)]
    if part.owner.detail==3:profile=[profile[i] for i in (0,2,5,7,12)]
    part.spindle(a,b,[(t,r*radius,mat) for t,r,mat in profile],part.n(54,32,15,10))
    if part.owner.detail<=1:
        # Three dimples on the hub make rotation legible without a painted animation.
        for k in around(3):
            yy=y+radius*.45*cos(k); zz=z+radius*.45*sin(k)
            part.rod((x+side*width*.595,yy,zz),(x+side*width*.618,yy,zz),radius*.035,DARK_METAL,7)
