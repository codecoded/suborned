"""Blender background checks for the real normal export path and pilot outputs.
Blender -b -t 2 -P tools/art-quality/verify.py -- out/scratch/codex/material-pilot
"""
import importlib.util
import hashlib
import json
import sys
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
if not OUT.is_relative_to(ROOT/'out/scratch'):raise ValueError('Scratch output required')
spec=importlib.util.spec_from_file_location('figure_export',ROOT/'tools/figures/build.py')
export=importlib.util.module_from_spec(spec);spec.loader.exec_module(export)
test=OUT/'codec-check';test.mkdir(exist_ok=True)
# Use 1024 px: this is the actual threshold that previously forced PNG normals to JPEG.
img=bpy.data.images.new('normal-source',width=1024,height=1024,alpha=False)
img.colorspace_settings.name='Non-Color';img.generated_color=(.5,.5,1,1)
src=test/'source.png';img.filepath_raw=str(src);img.file_format='PNG';img.save()
img=bpy.data.images.load(str(src));img.colorspace_settings.name='Non-Color'
rel,size=export.write_tex('maps','full',img,1024,'normal',str(test))
assert rel.endswith('.png') and (test/rel).read_bytes()==src.read_bytes(), 'Full normal must preserve source bytes'
rel,size=export.write_tex('maps','small',img,512,'normal',str(test))
assert rel.endswith('.png') and (test/rel).read_bytes().startswith(b'\x89PNG'), 'Resized normal must be lossless PNG'
assert size==512 and 'sRGBTexture: 0' in Path(str(test/rel)+'.meta').read_text()
# PNG albedo policy still uses JPEG for a large opaque image.
rel,_=export.write_tex('maps','colour',img,1024,'albedo',str(test))
assert rel.endswith('.jpg')
M=json.loads((OUT/'manifest.json').read_text())
for s in M['sources']:
    assert hashlib.sha256((ROOT/s['path']).read_bytes()).hexdigest()==s['sha256'],s['path']
assert len(M['maps'])==5 and len(M['materials'])==5
assert len(json.loads((OUT/'renders.json').read_text()))==18
result={'normalSourceBytesPreserved':True,'resizedNormalLossless':True,'normalLinearImport':True,'colourCompressionUnchanged':True,'pilotSourcesUnchanged':True,'matchedRenders':18}
(OUT/'checks.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
