# overlay proof: each clip at the play camera's scale (38.571 px/m, 38 deg down, seen from the front 20 deg off), drawn 4x; the frames where the hips are
# furthest left and right overlaid (blue, red) over frame 0 (grey). Left panel: as the clip has it (Bake Into Pose: what the new metas give). Right: the body's
# centre held still (hips' XY offset removed every frame: what the unbaked metas gave with applyRootMotion off, approximately: Unity holds the centre of mass).
import json, math, sys
from PIL import Image, ImageDraw
src, out = sys.argv[1], sys.argv[2]
D = json.load(open(src)); PPM = 1080 / 28.0; K = 4; P = math.radians(38); Y = math.radians(20)
d = (math.sin(Y), math.cos(Y)); r = (math.cos(Y), -math.sin(Y))
SEG = [('Hips','Spine1'),('Spine1','Neck'),('Neck','Head'),('Neck','LeftArm'),('LeftArm','LeftForeArm'),('LeftForeArm','LeftHand'),('Neck','RightArm'),('RightArm','RightForeArm'),('RightForeArm','RightHand'),
       ('Hips','LeftUpLeg'),('LeftUpLeg','LeftLeg'),('LeftLeg','LeftFoot'),('LeftFoot','LeftToeBase'),('Hips','RightUpLeg'),('RightUpLeg','RightLeg'),('RightLeg','RightFoot'),('RightFoot','RightToeBase')]
def proj(p): return (p[0]*r[0] + p[1]*r[1], (p[0]*d[0] + p[1]*d[1]) * math.sin(P) + p[2] * math.cos(P))
names = list(D); PW, PH = int(1.0 * PPM * K), int(2.1 * PPM * K)
img = Image.new('RGB', (len(names) * (2 * PW + 20) + 10, PH + 60), (236, 232, 222)); dr = ImageDraw.Draw(img)
for ci, n in enumerate(names):
    F = D[n]; h0 = F[0]['Hips']
    xs = [f['Hips'][0] for f in F]; lo, hi = xs.index(min(xs)), xs.index(max(xs))
    for side in (0, 1):
        ox = 10 + ci * (2 * PW + 20) + side * PW
        dr.rectangle([ox, 30, ox + PW - 4, 30 + PH], outline=(160, 160, 150))
        gy = 30 + PH - int(0.25 * PPM * K)
        for fi, col, w in ((0, (150, 150, 150), 3), (lo, (40, 90, 220), 3), (hi, (220, 50, 50), 3)):
            f = F[fi]; off = (f['Hips'][0] - h0[0], f['Hips'][1] - h0[1]) if side else (0, 0)
            def S(b):
                p = f[b]; q = proj((p[0] - off[0] - h0[0], p[1] - off[1] - h0[1], p[2])); return (ox + PW / 2 + q[0] * PPM * K, gy - q[1] * PPM * K)
            for a, b in SEG:
                if a in f and b in f: dr.line([S(a), S(b)], fill=col, width=w)
            for b in ('LeftToeBase', 'RightToeBase', 'LeftFoot', 'RightFoot'):
                x, y = S(b); dr.ellipse([x - 5, y - 5, x + 5, y + 5], outline=col, width=2)
        # foot trail across all frames
        for b in ('LeftToeBase', 'RightToeBase'):
            pts = []
            for f in F:
                off = (f['Hips'][0] - h0[0], f['Hips'][1] - h0[1]) if side else (0, 0); p = f[b]; q = proj((p[0] - off[0] - h0[0], p[1] - off[1] - h0[1], p[2]))
                pts.append((ox + PW / 2 + q[0] * PPM * K, gy - q[1] * PPM * K))
            dr.line(pts, fill=(20, 140, 60), width=2)
        side_m = (max(xs) - min(xs))
        dr.text((ox + 4, 34), ('baked (new)' if side == 0 else 'centre held (0.2.122)'), fill=(0, 0, 0))
        if side == 0: dr.text((ox + 4, 46), f'hips {side_m*100:.1f} cm = {side_m*PPM:.1f} px', fill=(0, 0, 0))
    dr.text((10 + ci * (2 * PW + 20), 8), n.replace('.fbx', ''), fill=(0, 0, 0))
dr.text((10, PH + 40), 'play scale 38.571 px/m drawn 4x; grey frame 0, blue/red the hips furthest each way; green the toes over the whole clip', fill=(0, 0, 0))
img.save(out); print(out, img.size)
