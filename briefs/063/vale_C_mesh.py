# Forma, brief 063: Vale C image to 3D. Hunyuan 3.1 pro, the settings the D-cast used (Citizens3D/PROVENANCE.json): Normal, PBR, 50k faces.
#   python3 -I briefs/063/vale_mesh.py <take>     input = briefs/063/vale_C_front_input.png (front cut-out of the Codex model sheet, 1024 x 2048 RGBA)
# Pass 063-portraits/vale-model, budget $3.00 (Aster's go 2026-10-25); at most 2 takes; est $0.675 each. Output: paid/063-portraits/meshes/
import sys, base64
sys.path.insert(0, '/Users/sky/Projects/suborned/tools/paid'); import paid
R = '/Users/sky/Projects/suborned/'; take = sys.argv[1]
img = 'data:image/png;base64,' + base64.b64encode(open(R + 'briefs/063/vale_C_front_input.png', 'rb').read()).decode()
p = paid.Pass('063-portraits', 'vale-model')
p.mesh({'model': 'tencent:hunyuan-3d@3.1-pro', 'inputs': {'images': [img]}, 'settings': {'generateType': 'Normal', 'pbr': True, 'faceCount': 50000}},
       name=f'vale_C__hunyuan_pro_t{take}.glb', est=0.675, prompt='image-to-3D from the front cut-out of the Vale C model sheet (Codex, ledger sheets/vale_C_sheet_codex.png)',
       refs_log=[R + 'briefs/063/vale_C_front_input.png'], note=f'take {take}', used_in=['briefs/063-portraits.md'])
