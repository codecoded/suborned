# comparison boards: for each scene, the three directions' 2x crops stacked with a label band
from PIL import Image, ImageDraw, ImageFont
S=['pill-states','help-pip','listen-chip','walkin','call-window','cancel-warn','cancel-pop','guard-back','tp-fire']
F=ImageFont.truetype('/Users/sky/Projects/suborned/unity/Assets/Suborned/UI/Resources/Kit/fonts/Jost-Bold.ttf',30)
L={'a':'A  Chrome plate','b':'B  Station sign','c':'C  Orb and label'}
for i,s in enumerate(S,1):
    ims=[Image.open(f'crop-{d}-{s}.png').convert('RGB') for d in 'abc']
    W=max(im.width for im in ims); band=46; H=sum(im.height+band for im in ims)
    out=Image.new('RGB',(W,H),(247,244,236)); y=0
    for d,im in zip('abc',ims):
        ImageDraw.Draw(out).text((14,y+6),L[d],font=F,fill=(23,48,47))
        out.paste(im,(0,y+band)); y+=band+im.height
    out.save(f'../board-{i:02d}-{s}.png')
