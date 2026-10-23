# Removes the 0.2.56 control-lab overlays from the built shots (cv2 inpaint) so the new overlays sit on a clean street.
import cv2, numpy as np, sys
S='/Users/sky/Projects/suborned/briefs/043/build/'
R={ # name: rects (x0,y0,x1,y1) of the old overlays, with margin
 'listen-chip':[(845,240,1077,347)],
 'help-pip':[(845,302,1077,409),(748,411,1172,449)],
 'call-window':[(868,373,1052,428),(924,423,996,450)],
 'cancel-warn':[(854,377,1064,411),(786,410,1131,450),(944,446,974,460)],
 'cancel-pop':[(857,441,1064,474)],
 'guard-back':[(899,447,1022,480)],
 'walkin':[(856,342,1064,409),(826,409,1096,450),(944,446,974,460)],
 'tp-fire':[(886,460,1054,500),(956,494,984,508)],
}
for n,rs in R.items():
    im=cv2.imread(S+'done-'+n+'.png'); m=np.zeros(im.shape[:2],np.uint8)
    for x0,y0,x1,y1 in rs: m[y0:y1,x0:x1]=255
    out=cv2.inpaint(im,m,7,cv2.INPAINT_TELEA)
    cv2.imwrite('clean/'+n+'.png',out)
