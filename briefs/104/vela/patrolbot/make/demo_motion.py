"""Brief 104 (Vela): PatrolBot104's demo timeline, as the game would drive the parts. Pure Python (no bpy), so the no-slide check runs outside Blender:
  python3 -I demo_motion.py          prints the beats and checks that the ball's roll matches the distance (angle = distance / radius)

state(f) -> one frame's pose, all in the robot's rest frame (Blender axes: front = -y, the robot's left = +x, up = +z) or the world:
  pos (x, y)       the ball's ground contact in the world
  yaw_body         the body's heading (deg; 0 = facing world -y, + turns toward +x)
  yaw_ball, roll   the ball's heading and its roll about its own axle (rad; + = rolls forward); roll = distance travelled / R
  tilt (x, y)      the body's lean about its pivot (the top of the ball), rad, toward that world direction: speed and acceleration (into travel, into the turn)
                   through a damped spring, so the stop wobbles; shots and the hit kick it
  arm_swing, arm_raise, panel, slide, face, beacon, phase, flash, spark   the parts and the signals
"""

import math

FPS, FRAMES, R = 30, 600, 0.20  # the ball's radius (m): it is 0.40 across, centre 0.20 up
ARRIVE = ((-4.6, 1.8), (-2.4, 0.0), (0.0, 0.0))  # quadratic Bezier, ends moving +x (yaw 90)
LEAVE = ((0.0, 0.0), (1.5, 2.6), (5.0, 4.4))     # starts facing yaw 150 (away, to the right)
T_ARRIVE, T_BRAKE = 150, 36                       # frames: the patrol ends at 150, braking over the last 36
F_LEAVE, V_LEAVE, A_LEAVE = 490, 1.3, 1.1          # it starts rolling away at 490, to 1.3 m/s at 1.1 m/s^2

# beats (frames)
BEATS = [(0, "patrol"), (150, "stop, settle"), (178, "look around"), (258, "alarm"), (290, "turn to target"), (315, "panel opens"), (330, "sidearm out"),
         (350, "shot 1"), (362, "shot 2"), (374, "shot 3"), (392, "hit"), (435, "sidearm back"), (447, "panel closes"), (462, "stand down"), (465, "turn"), (490, "roll away")]
SHOTS, HIT = (350, 362, 374), 392
FACES = [(0, "calm"), (175, "curious"), (258, "alert"), (300, "focused"), (HIT, "hurt"), (440, "alert"), (462, "calm")]
BEACON = (258, 462)
# body heading keys while stopped (frame, deg); before 150 and after 490 it follows the travel
YAW_KEYS = [(150, 90.0), (178, 90.0), (198, 132.0), (214, 132.0), (234, 52.0), (248, 52.0), (262, 90.0), (290, 90.0), (312, 0.0), (465, 0.0), (490, 150.0)]


def smooth(x):
    x = min(max(x, 0.0), 1.0)
    return x * x * (3 - 2 * x)


def ramp(f, a, b):
    return smooth((f - a) / (b - a))


def keys(f, ks):
    if f <= ks[0][0]:
        return ks[0][1]
    for (fa, va), (fb, vb) in zip(ks, ks[1:]):
        if f <= fb:
            return va + (vb - va) * ramp(f, fa, fb)
    return ks[-1][1]


class Path:
    """A quadratic Bezier by arc length; past its end it carries on along the end tangent."""

    def __init__(self, p, n=600):
        self.p = p
        pts = [self.at(i / n) for i in range(n + 1)]
        self.pts, self.s = pts, [0.0]
        for a, b in zip(pts, pts[1:]):
            self.s.append(self.s[-1] + math.dist(a, b))
        self.length = self.s[-1]

    def at(self, t):
        (ax, ay), (bx, by), (cx, cy) = self.p
        u = 1 - t
        return (u * u * ax + 2 * u * t * bx + t * t * cx, u * u * ay + 2 * u * t * by + t * t * cy)

    def pos(self, s):
        if s <= 0:
            return self.pts[0]
        if s >= self.length:
            (ax, ay), (bx, by) = self.pts[-2], self.pts[-1]
            d = math.dist((ax, ay), (bx, by))
            k = (s - self.length) / d
            return (bx + (bx - ax) * k, by + (by - ay) * k)
        lo, hi = 0, len(self.s) - 1
        while hi - lo > 1:
            mid = (lo + hi) // 2
            lo, hi = (mid, hi) if self.s[mid] <= s else (lo, mid)
        k = (s - self.s[lo]) / (self.s[hi] - self.s[lo])
        (ax, ay), (bx, by) = self.pts[lo], self.pts[hi]
        return (ax + (bx - ax) * k, ay + (by - ay) * k)


P_IN, P_OUT = Path(ARRIVE), Path(LEAVE)
V_IN = P_IN.length / ((T_ARRIVE - T_BRAKE / 2) / FPS)  # constant patrol speed, then a linear brake to a stop at T_ARRIVE


def dist_in(f):
    t, tb, T = f / FPS, (T_ARRIVE - T_BRAKE) / FPS, T_ARRIVE / FPS
    if t <= tb:
        return V_IN * t
    if t >= T:
        return P_IN.length
    d = T - tb
    x = t - tb
    return V_IN * tb + V_IN * (x - x * x / (2 * d))


def dist_out(f):
    t = max(f - F_LEAVE, 0) / FPS
    ta = V_LEAVE / A_LEAVE
    return 0.5 * A_LEAVE * t * t if t <= ta else 0.5 * A_LEAVE * ta * ta + V_LEAVE * (t - ta)


def where(f):
    """(pos, distance travelled so far)."""
    if f <= T_ARRIVE:
        return P_IN.pos(dist_in(f)), dist_in(f)
    return P_OUT.pos(dist_out(f)), P_IN.length + dist_out(f)


def travel_yaw(v):
    return math.degrees(math.atan2(v[0], -v[1]))


def _diff(f, h=0.5):
    (x0, y0), _ = where(f - h)
    (x1, y1), _ = where(f + h)
    return ((x1 - x0) * FPS / (2 * h), (y1 - y0) * FPS / (2 * h))


def build():
    """Every frame's state, in order (the lean spring integrates through the frames)."""
    out, tilt, tv = [], [0.0, 0.0], [0.0, 0.0]
    w, zeta, sub = 2 * math.pi * 1.5, 0.24, 8
    k_v, k_t, k_n = 0.035, 0.05, 0.22  # rad per m/s; rad per m/s^2 along the travel and across it (into the turn)
    last_yaw = travel_yaw(_diff(1))
    for f in range(FRAMES + 1):
        (x, y), s = where(f)
        v, v2 = _diff(max(f, 1)), _diff(max(f, 1) + 1)  # frame 0: already rolling at patrol speed
        a = ((v2[0] - v[0]) * FPS, (v2[1] - v[1]) * FPS)
        sp = math.hypot(*v)
        moving = sp > 0.02
        if f == F_LEAVE:  # set off along the leave path's start
            last_yaw = travel_yaw((LEAVE[1][0] - LEAVE[0][0], LEAVE[1][1] - LEAVE[0][1]))
        if moving:
            last_yaw = travel_yaw(v)
        if f < T_ARRIVE or f >= F_LEAVE:
            yaw_body = last_yaw
        else:
            yaw_body = keys(f, YAW_KEYS)
        yaw_ball = last_yaw if moving or f <= T_ARRIVE else keys(f, [(465, 90.0), (490, 150.0)])
        # target lean: into travel with speed, and with the acceleration split along / across the travel
        tgt = [0.0, 0.0]
        if sp > 1e-6:
            ux, uy = v[0] / sp, v[1] / sp
            at, an = a[0] * ux + a[1] * uy, -a[0] * uy + a[1] * ux  # an: + = toward the travel's left
            tgt = [k_v * v[0] + k_t * at * ux + k_n * an * -uy, k_v * v[1] + k_t * at * uy + k_n * an * ux]
        yb = math.radians(yaw_body)
        back = (-math.sin(yb), math.cos(yb))  # the body's back, in the world
        kick = 0.0
        if f in SHOTS:
            kick = 0.9
        if f == HIT:
            kick = 1.5
        tv[0] += kick * back[0]
        tv[1] += kick * back[1]
        if f == HIT:  # hit on its right side: pushed a little to its left too
            tv[0] += 0.5 * math.cos(yb)
            tv[1] += 0.5 * math.sin(yb)
        if f == 0:  # it rolls in already leaning at its patrol lean
            tilt = list(tgt)
        for _ in range(sub):
            dt = 1 / FPS / sub
            for i in range(2):
                tv[i] += (-w * w * (tilt[i] - tgt[i]) - 2 * zeta * w * tv[i]) * dt
                tilt[i] += tv[i] * dt
        t = f / FPS
        frac = min(sp / V_IN, 1.0)
        recoil = sum(math.exp(-(f - k) / 2.0) for k in SHOTS if 0 <= f - k < 8)
        flinch = math.exp(-(f - HIT) / 4.0) if 0 <= f - HIT < 20 else 0.0
        out.append(dict(
            f=f, pos=(x, y), dist=s, speed=sp, yaw_body=yaw_body, yaw_ball=yaw_ball, roll=s / R, tilt=tuple(tilt),
            arm_swing=math.radians(7.0) * frac * math.sin(2 * math.pi * 1.25 * t),
            arm_raise=math.radians(12.0) * (ramp(f, 258, 272) - ramp(f, 448, 465)) + math.radians(22.0) * flinch,
            panel=150.0 * (ramp(f, 315, 330) - ramp(f, 447, 462)),
            slide=0.11 * (ramp(f, 330, 342) - ramp(f, 435, 447)) - 0.018 * recoil,
            face=[n for k, n in FACES if f >= k][-1],
            beacon=BEACON[0] <= f < BEACON[1], phase=2 * math.pi * 1.6 * t,
            flash=any(0 <= f - k < 2 for k in SHOTS), spark=0 <= f - HIT < 3,
        ))
    return out


def check(states):
    """The ball rolls without sliding: roll x R matches the distance moved along the ground, frame by frame."""
    worst = 0.0
    for a, b in zip(states, states[1:]):
        moved = math.dist(a["pos"], b["pos"])
        rolled = (b["roll"] - a["roll"]) * R
        worst = max(worst, abs(moved - rolled))
    return worst


if __name__ == "__main__":
    st = build()
    print(f"patrol path {P_IN.length:.2f} m at {V_IN:.2f} m/s; leave path {P_OUT.length:.2f} m to {V_LEAVE} m/s")
    for f, name in BEATS:
        s = st[f]
        print(f"{f:4d} {f / FPS:5.2f}s {name:16s} pos ({s['pos'][0]:+.2f}, {s['pos'][1]:+.2f}) yaw {s['yaw_body']:6.1f} "
              f"tilt {math.degrees(math.hypot(*s['tilt'])):4.1f} deg  face {s['face']}")
    peak = max(math.degrees(math.hypot(*s["tilt"])) for s in st)
    print(f"peak lean {peak:.1f} deg; worst slide per frame {check(st) * 1000:.3f} mm")
