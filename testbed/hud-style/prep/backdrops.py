# HUD style lab backdrops: the 0.2.56 shots (control-lab overlays already inpainted out, briefs/044/src/clean) with the
# whole HUD (roster, clock bar, feed card, minimap, goal line) inpainted out too, so the lab draws every HUD piece live.
# Run: python3 testbed/hud-style/prep/backdrops.py   (needs opencv + pillow)
import cv2, numpy as np
from PIL import Image
S='/Users/sky/Projects/suborned/briefs/044/src/clean/'
O='/Users/sky/Projects/suborned/testbed/hud-style/bg/'
common=[(6,6,112,352),(104,46,284,88),(10,352,150,392),(1405,6,1914,66),(1798,66,1910,106),(6,926,236,1072),(1512,946,1914,1030),(1856,1030,1908,1070),(880,1026,1040,1066)]
extra={'call-window':[(800,1026,1120,1066)],'tp-fire':[(690,1030,1230,1066),(234,984,320,1016)],'guard-back':[(1512,900,1914,1035),(1780,910,1914,950),(800,1026,1120,1066)],
       'cancel-warn':[(800,1026,1120,1066)],'walkin':[]}
for n in ['help-pip','listen-chip','walkin','call-window','cancel-warn','cancel-pop','guard-back','tp-fire']:
    im=cv2.imread(S+n+'.png'); m=np.zeros(im.shape[:2],np.uint8)
    for x0,y0,x1,y1 in common+extra.get(n,[]): m[y0:y1,x0:x1]=255
    out=cv2.inpaint(im,m,6,cv2.INPAINT_TELEA)
    cv2.imwrite(O+n+'.jpg',out,[cv2.IMWRITE_JPEG_QUALITY,90]); print(n)
