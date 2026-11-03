import json,struct,sys,os
for p in sys.argv[1:]:
    b=open(p,'rb').read()
    ln=struct.unpack('<I',b[12:16])[0]; j=json.loads(b[20:20+ln])
    acc=j['accessors']; tris=0; prims=0; mats=set(); nodes_with_mesh=sum(1 for n in j.get('nodes',[]) if 'mesh' in n)
    for m in j['meshes']:
        for pr in m['primitives']:
            prims+=1; mats.add(pr.get('material'))
            tris+= (acc[pr['indices']]['count'] if 'indices' in pr else acc[pr['attributes']['POSITION']]['count'])//3
    imgs=len(j.get('images',[])); skins=len(j.get('skins',[]))
    print(f"{os.path.basename(p)}: tris {tris} primitives {prims} materials {len(mats)} meshNodes {nodes_with_mesh} images {imgs} skins {skins}")
