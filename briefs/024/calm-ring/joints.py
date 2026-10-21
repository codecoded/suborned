import cv2, numpy as np

def joints(im, hole, region, dark=9, angtol=7, verbose=False):
    """Draw tile joints through `hole` (uint8 mask 255). `region` = (x0,y0,x1,y1) where pavement lines are looked for."""
    H, W = hole.shape
    g = cv2.cvtColor(im, cv2.COLOR_BGR2GRAY)
    bg = cv2.medianBlur(g, 9)
    line = ((bg.astype(np.int16) - g.astype(np.int16)) > dark).astype(np.uint8) * 255
    x0, y0, x1, y1 = region
    reg = np.zeros_like(line); reg[y0:y1, x0:x1] = 255
    ban = cv2.dilate(hole, np.ones((9, 9), np.uint8))
    line = cv2.bitwise_and(line, cv2.bitwise_not(ban)); line = cv2.bitwise_and(line, reg)
    segs = cv2.HoughLinesP(line, 1, np.pi / 720, 38, minLineLength=45, maxLineGap=5)
    if segs is None: return im
    segs = segs.reshape(-1, 4).astype(np.float32)
    ang = (np.degrees(np.arctan2(segs[:, 3] - segs[:, 1], segs[:, 2] - segs[:, 0])) % 180)
    # dominant directions
    hist, edges = np.histogram(ang, bins=180, range=(0, 180))
    k = cv2.GaussianBlur(hist.astype(np.float32).reshape(-1, 1), (1, 9), 2.5).ravel()
    d1 = int(np.argmax(k)); k2 = k.copy()
    for d in range(-30, 31): k2[(d1 + d) % 180] = 0
    d2 = int(np.argmax(k2))
    if verbose: print("dirs", d1, d2, len(segs))
    out = im.copy().astype(np.float32)
    col = np.median(im[line > 0].reshape(-1, 3), axis=0) if (line > 0).any() else np.array([40, 40, 40])
    # use a dark joint colour from the line pixels themselves
    ys, xs = np.nonzero(line)
    col = np.percentile(im[ys, xs].astype(np.float32), 30, axis=0)
    def cls(a):
        for i, d in enumerate((d1, d2)):
            if min(abs(a - d), 180 - abs(a - d)) <= angtol: return i
        return -1
    drawn = [np.zeros((H, W), np.uint8), np.zeros((H, W), np.uint8)]
    # merge segments of the same line (same class, near rho)
    lines = [[], []]
    for (ax, ay, bx, by), a in zip(segs, ang):
        c = cls(a)
        if c < 0: continue
        th = np.radians(d1 if c == 0 else d2)
        nx, ny = -np.sin(th), np.cos(th)
        rho = ax * nx + ay * ny
        lines[c].append((rho, ax, ay, bx, by))
    def merged(cl):
        cl.sort()
        groups = []
        for it in cl:
            if groups and abs(it[0] - groups[-1][-1][0]) < 2.5: groups[-1].append(it)
            else: groups.append([it])
        return groups
    # which class is the "long" one: the one with longer total length
    tot = [sum(np.hypot(i[3] - i[1], i[4] - i[2]) for i in lines[c]) for c in (0, 1)]
    long_c = 0 if tot[0] >= tot[1] else 1
    for c in (long_c, 1 - long_c):
        th = np.radians(d1 if c == 0 else d2)
        dx, dy = np.cos(th), np.sin(th)
        for grp in merged(lines[c]):
            pts = np.array([[g_[1], g_[2]] for g_ in grp] + [[g_[3], g_[4]] for g_ in grp])
            rho = np.mean([g_[0] for g_ in grp])
            cx, cy = pts.mean(0)
            # extent known along the line
            t = (pts - [cx, cy]) @ [dx, dy]
            tmin, tmax = t.min(), t.max()
            # ray both ways from the known extent through the hole only
            tmp = np.zeros((H, W), np.uint8)
            P1 = (int(round(cx + dx * (tmin - 1500))), int(round(cy + dy * (tmin - 1500))))
            P2 = (int(round(cx + dx * (tmax + 1500))), int(round(cy + dy * (tmax + 1500))))
            cv2.line(tmp, P1, P2, 255, 1)
            tmp = cv2.bitwise_and(tmp, hole)
            if c != long_c:
                # cross joints stop at the first long joint
                tmp = cv2.bitwise_and(tmp, cv2.bitwise_not(cv2.dilate(drawn[long_c], np.ones((3, 3), np.uint8))))
            if not tmp.any(): continue
            # only if the line runs into the hole from known pavement within 14 px of a hole edge (support)
            drawn[c] = cv2.bitwise_or(drawn[c], tmp)
    allm = cv2.bitwise_or(drawn[0], drawn[1]).astype(np.float32) / 255
    allm = cv2.GaussianBlur(allm, (0, 0), 0.6) * 1.8
    allm = np.clip(allm, 0, 1)[:, :, None] * 0.85
    out = out * (1 - allm) + col.astype(np.float32) * allm
    return np.clip(out, 0, 255).astype(np.uint8)
