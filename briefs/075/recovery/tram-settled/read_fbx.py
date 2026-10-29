"""Read-only binary FBX bounds and transform metadata; no Blender/Unity launch."""
import struct,zlib,json,pathlib
p=pathlib.Path('unity/Assets/Suborned/OldTownArt/DecoKit/Models/Ground/DK_Tile_TramRail.fbx');b=p.read_bytes();ver=struct.unpack_from('<I',b,23)[0];wide=ver>=7500;fmt='<QQQB' if wide else '<IIIB';hdr=25 if wide else 13
stats=[]
payload={}
def prop(pos):
 t=chr(b[pos]);pos+=1
 if t in 'YCBIFDL':
  f={'Y':'h','C':'?','B':'b','I':'i','F':'f','D':'d','L':'q'}[t];v=struct.unpack_from('<'+f,b,pos)[0];return v,pos+struct.calcsize(f)
 if t in 'SR':
  n=struct.unpack_from('<I',b,pos)[0];pos+=4;v=b[pos:pos+n];return (v.decode(errors='replace') if t=='S' else v.hex()),pos+n
 if t in 'fdlibc':
  n,enc,size=struct.unpack_from('<III',b,pos);pos+=12;raw=b[pos:pos+size];raw=zlib.decompress(raw) if enc else raw
  f={'f':'f','d':'d','l':'q','i':'i','b':'?','c':'b'}[t];return list(struct.unpack('<'+str(n)+f,raw)),pos+size
 raise ValueError(t)
def node(pos,parent=''):
 end,n,_,length=struct.unpack_from(fmt,b,pos)
 if end==0:return 0
 pos+=hdr;name=b[pos:pos+length].decode();pos+=length;values=[]
 for _ in range(n):v,pos=prop(pos);values.append(v)
 if name in ['Vertices','PolygonVertexIndex']:payload[name]=values[0]
 if name=='Vertices':
  v=values[0];stats.append({'node':parent,'vertices':len(v)//3,'bounds':[ [min(v[i::3]),max(v[i::3])] for i in range(3)]})
 if name=='P' and any(s in str(values[0]) for s in ['Lcl','Geometric','UnitScale','Axis']):stats.append({'node':parent,'property':values})
 while pos<end-hdr:
  following=node(pos,parent+'/'+name)
  if not following:break
  pos=following
 return end
pos=27
while pos<len(b)-hdr:
 n=node(pos)
 if not n:break
 pos=n
out={'source':str(p),'version':ver,'records':stats};pathlib.Path('briefs/075/recovery/tram-settled/fbx-source.json').write_text(json.dumps(out,indent=2)+'\n');pathlib.Path('out/scratch/latch/tram-settled/fbx-mesh.json').write_text(json.dumps(payload));print('read-only FBX bounds/payload recorded')
