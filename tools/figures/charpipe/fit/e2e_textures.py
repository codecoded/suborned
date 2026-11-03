"""Brief 069 end-to-end: each figure's delivered albedo, made from its generated glb's 4K base colour (the source stays in paid/): resized to the figure's
budget (fit.json budgets texPx, Lanczos), with any merged prop's colour patch painted into its free cell (the build's colourPatches), saved under the
name the FBX's material points at, plus a Unity texture meta with that size as its explicit import max size. System python (PIL); no Blender.
  [FIT_SET=e2e|fixer] [FIT_OUT=<out>] python3 tools/figures/charpipe/fit/e2e_textures.py     (run_e2e.sh calls it last)"""
import json, os, io, struct, hashlib, importlib.util
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(HERE))))
_s = importlib.util.spec_from_file_location('unitymeta', os.path.join(ROOT, 'tools/figures/unitymeta.py')); um = importlib.util.module_from_spec(_s); _s.loader.exec_module(um)   # read only
SET = os.environ.get('FIT_SET', 'e2e'); E = json.load(open(os.path.join(HERE, SET + '.json'))); OUT = os.path.join(ROOT, os.environ.get('FIT_OUT', E['out']), 'a'); rows = {}
for r in E['people']:
    b = open(os.path.join(ROOT, r['src']), 'rb').read()
    n = struct.unpack('<I', b[12:16])[0]; js = json.loads(b[20:20 + n]); off = 20 + n; bl = struct.unpack('<I', b[off:off + 4])[0]; binc = b[off + 8:off + 8 + bl]
    mat = js['materials'][0]; ti = mat['pbrMetallicRoughness']['baseColorTexture']['index']; im = js['images'][js['textures'][ti]['source']]
    bv = js['bufferViews'][im['bufferView']]; data = binc[bv.get('byteOffset', 0):bv.get('byteOffset', 0) + bv['byteLength']]
    ext = {'image/png': 'png', 'image/jpeg': 'jpg', 'image/webp': 'webp'}[im['mimeType']]
    st = json.load(open(os.path.join(OUT, f"{r['id']}_H.json"))); px = st.get('texPx') or 2048
    img = Image.open(io.BytesIO(data)).convert('RGB'); src_size = img.size
    if img.size[0] > px: img = img.resize((px, px), Image.LANCZOS)
    for p in st.get('colourPatches') or []:
        g = p['grid']; u, v = p['cell']; s = px / g
        x0, x1 = int(round(u * s)), int(round((u + 1) * s)); y1 = int(round(px - v * s)); y0 = int(round(px - (v + 1) * s))     # image rows run down, v runs up
        lin = p['rgbLinear']; srgb = tuple(int(round(255 * ((c * 12.92) if c <= 0.0031308 else (1.055 * c ** (1 / 2.4) - 0.055)))) for c in lin)
        img.paste(srgb, (x0, y0, x1, y1))
    name = f"{r['id']}_H_albedo.{ext}"; path = os.path.join(OUT, name); buf = io.BytesIO()
    img.save(buf, 'PNG' if ext == 'png' else 'JPEG', **({'optimize': False} if ext == 'png' else {'quality': 92, 'subsampling': 0}))
    open(path, 'wb').write(buf.getvalue())
    rel = f"Suborned/People/CharPipe/{SET}/{name}"; open(path + '.meta', 'w').write(um.tex_meta(rel, 'albedo', px))
    rows[r['id']] = {'file': name, 'meta': name + '.meta', 'sourcePx': list(src_size), 'px': px, 'maxTextureSize': px, 'patches': len(st.get('colourPatches') or []),
                     'bytes': len(buf.getvalue()), 'sha256': hashlib.sha256(buf.getvalue()).hexdigest()}
json.dump(rows, open(os.path.join(OUT, 'textures.json'), 'w'), indent=1, sort_keys=True); print(json.dumps(rows, indent=1))
