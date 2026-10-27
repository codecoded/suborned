"""Brief 069 end-to-end: intake sheets -> multi-view crops -> Hunyuan 3D 3.1 Pro on Runware (textured, not rigged), through tools/paid/paid.py.
  python3 -I tools/figures/charpipe/fit/e2e_mesh.py crops                      # $0: write the view crops (e2e/crops/<id>_<view>.png)
  python3 -I tools/figures/charpipe/fit/e2e_mesh.py open                       # open the pass (budget from e2e.json; refuses to raise)
  python3 -I tools/figures/charpipe/fit/e2e_mesh.py mesh <id> [--front-only] [--take N]
Views: each third of the sheet (front, the person's own left side, back) is cut out against the sheet's flat ground (intake_check.mask_of), trimmed to
the figure with a margin, padded square at e2e.json's size, background white. Runware's model page lists the multi-view angles in the order
left, right, back, top, bottom, left_front, right_front after the front; the sheet has no right view, so the right is the left mirrored (a person's
sides are near mirror images; asymmetric details then come from the front and back). The order is the page's listed order: inputs.images =
[front, left, right, back]. --front-only sends the front alone (how today's figures were made), the fallback if the multi-view set misreads."""
import json, os, sys, base64, io
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); CP = os.path.dirname(HERE); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(CP)))
sys.path.insert(0, CP); sys.path.insert(0, os.path.join(ROOT, 'tools/paid'))
E = json.load(open(os.path.join(HERE, os.environ.get('FIT_SET', 'e2e') + '.json'))); OUT = os.path.join(ROOT, E['out']); CROPS = os.path.join(OUT, 'crops')


def crops():
    import intake_check as ic
    os.makedirs(CROPS, exist_ok=True); rep = {}
    for r in E['people']:
        a = np.asarray(Image.open(os.path.join(ROOT, r['sheet'])).convert('RGB')); h, w, _ = a.shape
        m, bg, flat = ic.mask_of(a); views = {}
        # holes inside the figure (a coat's grey shading close to the sheet's ground) are figure, not ground: only ground reached from the
        # sheet's edge is ground (the Enforcer's back: the coat's grey gradient read as ground and left a white hole)
        from PIL import ImageDraw
        mi = Image.fromarray(np.where(m, 255, 0).astype(np.uint8)).copy(); H_, W_ = m.shape   # a writable copy: floodfill on the array-backed image did nothing
        for (x_, y_) in [(x, 0) for x in range(0, W_, 16)] + [(x, H_ - 1) for x in range(0, W_, 16)] + [(0, y) for y in range(0, H_, 16)] + [(W_ - 1, y) for y in range(0, H_, 16)]:
            if mi.getpixel((x_, y_)) == 0: ImageDraw.floodfill(mi, (x_, y_), 128)
        m = np.asarray(mi) != 128
        for k, (name, x0, x1) in enumerate((('front', 0, w // 3), ('left', w // 3, 2 * w // 3), ('back', 2 * w // 3, w))):
            sub = a[:, x0:x1]; ms = m[:, x0:x1]
            ys, xs = np.nonzero(ms); y0, y1, xa, xb = ys.min(), ys.max(), xs.min(), xs.max(); mg = E['crop']['marginPx']
            y0, y1 = max(0, y0 - mg), min(h - 1, y1 + mg); xa, xb = max(0, xa - mg), min(sub.shape[1] - 1, xb + mg)
            rgb = sub[y0:y1 + 1, xa:xb + 1].copy(); mk = ms[y0:y1 + 1, xa:xb + 1]
            rgb[~mk] = 255
            im = Image.fromarray(rgb); side = max(im.size); sq = Image.new('RGB', (side, side), (255, 255, 255)); sq.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
            sq = sq.resize((E['crop']['sizePx'], E['crop']['sizePx']), Image.LANCZOS); views[name] = sq
        views['right'] = views['left'].transpose(Image.FLIP_LEFT_RIGHT)
        for name, im in views.items(): im.save(os.path.join(CROPS, f"{r['id']}_{name}.png"))
        rep[r['id']] = {'ground': [round(float(x), 1) for x in bg], 'flat': round(flat, 2)}
    json.dump(rep, open(os.path.join(CROPS, 'crops.json'), 'w'), indent=1); print('crops', rep)


def uri(p):
    return 'data:image/png;base64,' + base64.b64encode(open(p, 'rb').read()).decode()


def mesh(pid, front_only, take):
    import paid
    r = {x['id']: x for x in E['people']}[pid]; M = E['model']
    order = ['front'] if front_only else M['viewOrder']
    files = [os.path.join(CROPS, f'{pid}_{v}.png') for v in order]
    p = paid.Pass(E['job'], E['pass'])
    settings = dict(M['settings'])
    if r.get('kind') in M.get('faceLimitByKind', {}): settings['faceLimit'] = M['faceLimitByKind'][r['kind']]       # the person's kind picks the face limit (data)
    p.mesh({'model': M['id'], 'inputs': {'images': [uri(f) for f in files]}, 'settings': settings, 'includeCost': True},
           name=os.path.basename(r['src']).replace('_t1.glb', f'_t{take}.glb') if not front_only else f"{pid}__front_t{take}.glb", est=M['estUsd'],
           prompt=f"image-to-3D, views {order}, from the intake sheet {r['sheet']}", refs_log=files,
           note=f"take {take}; {r['label']}", used_in=['briefs/069-character-pipeline.md'])


if __name__ == '__main__':
    a = sys.argv[1:]
    if a[0] == 'crops': crops()
    elif a[0] == 'open':
        import paid; paid.open_pass(E['job'], E['pass'], E['budgetUsd'], note=E['note'])
    elif a[0] == 'mesh': mesh(a[1], '--front-only' in a, int(a[a.index('--take') + 1]) if '--take' in a else 1)
