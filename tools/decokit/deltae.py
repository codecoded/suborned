"""CIE dE2000 between sRGB hex colours (D65, 2 degree; same maths as the palette review table). No bpy: imported by mats.py and tests."""
import math


def lab(h):
    h=h.lstrip('#'); r,g,b=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    f=lambda c: c/12.92 if c<=0.04045 else ((c+0.055)/1.055)**2.4
    r,g,b=map(f,(r,g,b))
    X=(0.4124*r+0.3576*g+0.1805*b)/0.95047;Y=0.2126*r+0.7152*g+0.0722*b;Z=(0.0193*r+0.1192*g+0.9505*b)/1.08883
    g_=lambda t: t**(1/3) if t>0.008856 else 7.787*t+16/116
    return (116*g_(Y)-16,500*(g_(X)-g_(Y)),200*(g_(Y)-g_(Z)))
def de(a,b):
    L1,a1,b1=lab(a);L2,a2,b2=lab(b)
    C1=math.hypot(a1,b1);C2=math.hypot(a2,b2);Cm=(C1+C2)/2
    G=0.5*(1-math.sqrt(Cm**7/(Cm**7+25**7)))
    a1p,a2p=(1+G)*a1,(1+G)*a2
    C1p,C2p=math.hypot(a1p,b1),math.hypot(a2p,b2)
    h1=math.degrees(math.atan2(b1,a1p))%360;h2=math.degrees(math.atan2(b2,a2p))%360
    dL=L2-L1;dC=C2p-C1p;dh=h2-h1
    if dh>180: dh-=360
    elif dh<-180: dh+=360
    dH=2*math.sqrt(C1p*C2p)*math.sin(math.radians(dh/2))
    Lp=(L1+L2)/2;Cp=(C1p+C2p)/2
    hp=(h1+h2)/2 if abs(h1-h2)<=180 else ((h1+h2+360)/2 if h1+h2<360 else (h1+h2-360)/2)
    T=1-0.17*math.cos(math.radians(hp-30))+0.24*math.cos(math.radians(2*hp))+0.32*math.cos(math.radians(3*hp+6))-0.2*math.cos(math.radians(4*hp-63))
    dth=30*math.exp(-((hp-275)/25)**2)
    Rc=2*math.sqrt(Cp**7/(Cp**7+25**7));Sl=1+0.015*(Lp-50)**2/math.sqrt(20+(Lp-50)**2)
    Rt=-math.sin(math.radians(2*dth))*Rc
    return math.sqrt((dL/Sl)**2+(dC/(1+0.045*Cp))**2+(dH/(1+0.015*Cp*T))**2+Rt*(dC/(1+0.045*Cp))*(dH/(1+0.015*Cp*T)))


def min_signal(hexstr, signals):
    """(smallest dE2000, name) of a colour against the signal colours."""
    return min((de(hexstr, h), k) for k, h in signals.items())
