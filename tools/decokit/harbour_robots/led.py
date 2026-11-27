"""Authoring textures for the live RobotFace glyphs. Unity drives the same layouts on its LED shader."""
from math import floor
from pathlib import Path
import json

FAMILY = {'Porter':'Porter', 'Clerk':'Clerk', 'Sweeper':'Clerk', 'Runner':'Porter'}
RIG = {'Porter':'RobotPorter', 'Clerk':'RobotShopkeeper'}
# Four columns, two rows, starting at the upper left. Each register is a discrete switch.
REGISTERS = ('open','half','shut','left','right','noticed','wary','reporting')


def cells(data, rig, state):
    width,height=rig['grid']
    lit=set()
    stages=rig['attention']['stages']
    expression=stages[{'noticed':1,'wary':2,'reporting':3}[state]] if state in ('noticed','wary','reporting') else rig['neutral']
    gaze=-2 if state=='left' else 2 if state=='right' else 0
    for feature in ('left','right','mouth'):
        name=expression[feature]
        if not name:continue
        glyph=data['glyphs'][name]
        rows=glyph['rows'][::-1];w,h=glyph['w'],glyph['h']
        dx,dy=rig['mouth'] if feature=='mouth' else rig['eye']
        if state=='reporting':dx,dy=0,0
        x=floor(width*.5+abs(dx)-w*.5+.5)
        if feature=='left':x=width-w-x
        if feature!='mouth':x+=gaze
        y=floor(height*.5+dy-h*.5+.5)
        for j,row in enumerate(rows):
            for i,char in enumerate(row):
                on=char=='#'
                if feature!='mouth' and state=='half':on=on and abs(j-(h-1)*.5)<=max(h*.25,.5)
                if feature!='mouth' and state=='shut':on=j==(h-1)//2 and any(line[i]=='#' for line in rows if i<len(line))
                if on and 0<=x+i<width and 0<=y+j<height:lit.add((x+i,height-1-(y+j)))
    return lit


def write(directory):
    from PIL import Image,ImageDraw
    data=json.loads((Path(__file__).resolve().parent.parent/'art/robots/robot-faces.json').read_text())
    side=12
    for family,kind in RIG.items():
        rig=data['robots'][kind];w,h=rig['grid'];tw,th=w*side,h*side
        image=Image.new('RGB',(4*tw,2*th),(7,14,14));pen=ImageDraw.Draw(image)
        for frame,state in enumerate(REGISTERS):
            lit=cells(data,rig,state)
            colour=(255,164,46) if family=='Porter' else (72,245,184)
            if state=='reporting':colour=(255,69,41)
            for row in range(h):
                for col in range(w):
                    x=frame%4*tw+col*side+side//2;y=frame//4*th+row*side+side//2
                    on=(col,row) in lit
                    pen.ellipse((x-5,y-5,x+5,y+5),fill=tuple(round(k*.32) for k in colour) if on else (13,23,23))
                    pen.ellipse((x-4,y-4,x+4,y+4),fill=colour if on else (19,32,31))
                    if on:pen.ellipse((x-2,y-2,x+1,y+1),fill=tuple(min(255,round(k*.78+61)) for k in colour))
        image.save(directory/(family+'Display.png'),optimize=True)
