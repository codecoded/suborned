import sys; sys.path.insert(0,"/Users/sky/Projects/suborned/briefs/058")
from glib import *
SHOT=R+"out/batch/shots/office-front-yaw210.png"
fr=Frame(SHOT, blot=[(928,416,996,450)])
C=(960,462)
segs=[dict(icon="loop"),dict(icon="disable"),dict(icon="fire",violent=True)]
ring_wheel(fr,C,segs,lit=0)
ring_pill(fr,C,0,tokens("Loop",[("time","38 s"),("trail","+1"),("lasts","2:00")]),lit=True)
ring_pill(fr,C,120,tokens("Disable",[("time","30 s"),("trail","+2"),("alert","1"),("in","14 s")]))
ring_pill(fr,C,240,tokens("Fire",[("time","2 s"),("trail","+2"),("alert","≥2")],loud=True))
fr.crop((420,250,1500,760)).convert("RGB").save(R+"out/scratch/glyph/t1.png")
