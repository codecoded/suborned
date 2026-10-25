# Forma, brief 063: Vale model sheet for option C (front, side, back, A-pose), ready to run once Sky picks B. Not an image-to-3D call.
#   python3 -I briefs/063/vale_sheet.py            Codex first ($0). It returned 401 on 2026-10-24 (ledger rows 1 and 11 of 063-portraits).
#   python3 -I briefs/063/vale_sheet.py --runware  Runware openai:gpt-image@2 with the C plate as the reference, pass 063-portraits/vale-sheet, budget $0.30,
#                                                  estimated $0.06 a take (the concepts cost $0.043 each). Needs the coordinator's or Sky's go: the coordinator said "no cost".
# Output: paid/063-portraits/sheets/. The next step (not run): Hunyuan 3.1 pro image-to-3D at $0.675 a take, then tools/figures (brief 063 §3).
import sys, subprocess
sys.path.insert(0, '/Users/sky/Projects/suborned/tools/paid'); import paid
R = '/Users/sky/Projects/suborned/'; PROMPT = open(R + 'briefs/063/vale_C_sheet_prompt.txt').read(); REF = R + 'briefs/063/concepts/vale_C.png'
if '--runware' in sys.argv:
    p = paid.Pass('063-portraits', 'vale-sheet', budget=0.30, note='Vale C model sheet, Runware gpt-image@2 with the C plate as reference')
    p.image('openai:gpt-image@2', PROMPT, 1536, 1024, name='vale_C_sheet.png', est=0.06, subdir='sheets', inputs={'referenceImages': [paid.data_uri(REF)]}, refs_log=[REF],
            note='Codex login 401', used_in=['briefs/063-portraits.md'])
else:
    paid.codex_image('063-portraits', 'vale-sheet', PROMPT, 'vale_C_sheet.png', refs=[REF], subdir='sheets')
