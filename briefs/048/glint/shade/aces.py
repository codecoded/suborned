import numpy as np
def s2l(c): c=np.asarray(c,float); return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
def l2s(c): c=np.clip(np.asarray(c,float),0,1); return np.where(c<=0.0031308,c*12.92,1.055*c**(1/2.4)-0.055)
K=0.87
def aces(x):
    x=np.asarray(x,float); return K*np.clip((x*(2.51*x+0.03))/(x*(2.43*x+0.59)+0.14),0,1)
def aces_inv(y):
    # solve y/K = (2.51x²+0.03x)/(2.43x²+0.59x+0.14) for x
    y=np.clip(np.asarray(y,float)/K,0,0.985)
    a=2.51-2.43*y; b=0.03-0.59*y; c=-0.14*y
    return (-b+np.sqrt(b*b-4*a*c))/(2*a)
if __name__=='__main__':
    ex=2**0.15
    for name,pre,obs in [('sky horizon',s2l([0xf1/255,0xe5/255,0xcc/255])*1.3,(0xdd,0xdb,0xce)),('sea haze',s2l([0xf4/255,0xe3/255,0xc3/255]),(0xd9,0xcb,0xaf))]:
        out=l2s(aces(pre*ex))*255
        print(name,pre.round(3),out.round(0),obs)
