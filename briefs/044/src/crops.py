import sys
from PIL import Image
# crop rects (x0,y0,x1,y1) per scene at 1x; the x2 renders are cropped at 2x
S={'pill-states':(340,120,1760,820),'help-pip':(700,270,1220,480),'listen-chip':(780,220,1140,370),'walkin':(760,290,1160,480),'call-window':(800,340,1120,480),'cancel-warn':(740,350,1180,480),'cancel-pop':(780,400,1140,520),'guard-back':(820,400,1100,520),'tp-fire':(800,420,1140,540)}
for d in sys.argv[1:]:
    for k,(a,b,c,e) in S.items():
        im=Image.open(f'x2-{d}-{k}.png').crop((a*2,b*2,c*2,e*2)); im.save(f'crop-{d}-{k}.png')
