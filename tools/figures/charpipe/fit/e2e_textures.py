"""Brief 069 end-to-end: each figure's base-colour texture, copied byte for byte out of its generated glb (no re-encode), next to its FBX, under the
image name the FBX's material points at. System python, no Blender.
  [FIT_OUT=<out>] python3 tools/figures/charpipe/fit/e2e_textures.py    (run_e2e.sh calls it last)"""
import json, os, struct, hashlib
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(HERE))))
E = json.load(open(os.path.join(HERE, 'e2e.json'))); OUT = os.path.join(ROOT, os.environ.get('FIT_OUT', E['out']), 'a'); rows = {}
for r in E['people']:
    b = open(os.path.join(ROOT, r['src']), 'rb').read()
    n = struct.unpack('<I', b[12:16])[0]; js = json.loads(b[20:20 + n]); off = 20 + n; bl = struct.unpack('<I', b[off:off + 4])[0]; binc = b[off + 8:off + 8 + bl]
    mat = js['materials'][0]; ti = mat['pbrMetallicRoughness']['baseColorTexture']['index']; im = js['images'][js['textures'][ti]['source']]
    bv = js['bufferViews'][im['bufferView']]; data = binc[bv.get('byteOffset', 0):bv.get('byteOffset', 0) + bv['byteLength']]
    ext = {'image/png': 'png', 'image/jpeg': 'jpg', 'image/webp': 'webp'}[im['mimeType']]
    name = f"{r['id']}_H_albedo.{ext}"; open(os.path.join(OUT, name), 'wb').write(data)
    rows[r['id']] = {'file': name, 'glbImageName': im.get('name'), 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()}
json.dump(rows, open(os.path.join(OUT, 'textures.json'), 'w'), indent=1, sort_keys=True); print(json.dumps(rows, indent=1))
