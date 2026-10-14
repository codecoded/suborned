import numpy as np
def lin(c):
    c=np.asarray(c,float)/255.0
    return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
def lab(rgb):
    r,g,b=lin(rgb[...,0]),lin(rgb[...,1]),lin(rgb[...,2])
    f=lambda t: np.where(t>216/24389,np.cbrt(t),(24389/27*t+16)/116)
    x=f((0.4124564*r+0.3575761*g+0.1804375*b)/0.95047); y=f(0.2126729*r+0.7151522*g+0.072175*b); z=f((0.0193339*r+0.119192*g+0.9503041*b)/1.08883)
    return np.stack([116*y-16,500*(x-y),200*(y-z)],-1)
def de2000(a,b):
    """CIEDE2000, Sharma-Wu-Dalal, D65; same as tools/world/rules/deltae.ts. a,b: sRGB 0-255 arrays (...,3)."""
    L1,a1,b1=np.moveaxis(lab(np.asarray(a,float)),-1,0); L2,a2,b2=np.moveaxis(lab(np.asarray(b,float)),-1,0)
    rad=np.pi/180; deg=180/np.pi
    C1=np.hypot(a1,b1); C2=np.hypot(a2,b2); Cb=(C1+C2)/2; G=0.5*(1-np.sqrt(Cb**7/(Cb**7+25**7)))
    ap1=(1+G)*a1; ap2=(1+G)*a2; Cp1=np.hypot(ap1,b1); Cp2=np.hypot(ap2,b2)
    hp=lambda b,a: np.where((b==0)&(a==0),0,(np.arctan2(b,a)*deg+360)%360)
    hp1=hp(b1,ap1); hp2=hp(b2,ap2)
    dL=L2-L1; dC=Cp2-Cp1
    dh=hp2-hp1; dh=np.where(dh>180,dh-360,np.where(dh<-180,dh+360,dh)); dh=np.where(Cp1*Cp2==0,0,dh)
    dH=2*np.sqrt(Cp1*Cp2)*np.sin(dh/2*rad); Lb=(L1+L2)/2; Cpb=(Cp1+Cp2)/2
    hb=np.where(np.abs(hp1-hp2)<=180,(hp1+hp2)/2,np.where(hp1+hp2<360,(hp1+hp2+360)/2,(hp1+hp2-360)/2)); hb=np.where(Cp1*Cp2==0,hp1+hp2,hb)
    T=1-0.17*np.cos((hb-30)*rad)+0.24*np.cos(2*hb*rad)+0.32*np.cos((3*hb+6)*rad)-0.2*np.cos((4*hb-63)*rad)
    dTh=30*np.exp(-(((hb-275)/25)**2)); Rc=2*np.sqrt(Cpb**7/(Cpb**7+25**7)); Sl=1+(0.015*(Lb-50)**2)/np.sqrt(20+(Lb-50)**2)
    Sc=1+0.045*Cpb; Sh=1+0.015*Cpb*T; Rt=-np.sin(2*dTh*rad)*Rc
    return np.sqrt((dL/Sl)**2+(dC/Sc)**2+(dH/Sh)**2+Rt*(dC/Sc)*(dH/Sh))
def hex2rgb(h): h=h.lstrip('#'); return np.array([int(h[i:i+2],16) for i in (0,2,4)],float)
