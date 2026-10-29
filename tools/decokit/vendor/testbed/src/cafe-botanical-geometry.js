import * as THREE from '../vendor/three-0.186.1/three.module.min.js';

function atlasUV(region,u,v){return [(region%4+.012+u*.976)/4,(region<4?.5:0)+(.012+v*.976)/2];}
function geometry(positions,uvs,indices){
  const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));g.setAttribute('uv',new THREE.Float32BufferAttribute(uvs,2));
  g.setAttribute('uv1',g.attributes.uv.clone());g.setIndex(indices);g.computeVertexNormals();return g;
}
function detailLevel(detail){
  const level=detail===true?'inspection':detail===false?'close':detail;
  if(!['inspection','close','near','gameplay'].includes(level))throw new Error(`Unknown botanical detail: ${level}`);
  return level;
}

// Close leaves include a closed skin. Distant leaves keep the cubic bow and
// rolled edges but omit the subpixel thickness and its duplicate underside.
export function leafGeometry(kind,region,detail=true){
  const level=detailLevel(detail),broad=kind==='broad'||kind==='curled',close=level==='inspection';
  const rows=close?(kind==='fern'?4:kind==='ivy'?6:10):level==='close'?(kind==='curled'?4:kind==='fern'?2:3):(kind==='curled'?3:2),cols=close?(broad?4:2):level==='gameplay'?1:(broad?2:1);
  const positions=[],uvs=[],indices=[],frames=[];
  const curve=kind==='curled'?new THREE.CubicBezierCurve3(new THREE.Vector3(),new THREE.Vector3(.08,.30,.26),new THREE.Vector3(-.08,1.14,.44),new THREE.Vector3(-.035,.84,.14)):
    new THREE.CubicBezierCurve3(new THREE.Vector3(),new THREE.Vector3(-.035,.27,.24),new THREE.Vector3(.045,.74,.20),new THREE.Vector3(0,1,-.025));
  for(let j=0;j<=rows;j++){
    const t=j/rows,centre=curve.getPoint(t),tangent=curve.getTangent(t),across=new THREE.Vector3(1,0,0);
    across.addScaledVector(tangent,-across.dot(tangent)).normalize();if(kind==='curled')across.applyAxisAngle(tangent,t*t*Math.PI*.62);
    const normal=new THREE.Vector3().crossVectors(across,tangent).normalize(),breadth=kind==='ivy'?Math.sin(Math.PI*t)**.62*(1+.18*Math.sin(t*Math.PI*3)):Math.sin(Math.PI*t)**.52;
    for(let i=0;i<=cols;i++){
      const a=i/cols*2-1,vertex=centre.clone().addScaledVector(across,a*breadth*.5).addScaledVector(normal,-(broad?.16:.08)*a*a*breadth);
      positions.push(...vertex.toArray());frames.push(normal.clone());uvs.push(...atlasUV(region,i/cols,t));
      if(j<rows&&i<cols){const n=j*(cols+1)+i;if(j>0)indices.push(n,n+1,n+cols+2);if(j<rows-1)indices.push(n,n+cols+2,n+cols+1);}
    }
  }
  if(broad&&close){
    const count=positions.length/3,topIndices=[...indices],topUVs=[...uvs];
    for(let i=0;i<count;i++){
      positions.push(positions[i*3]-frames[i].x*.007,positions[i*3+1]-frames[i].y*.007,positions[i*3+2]-frames[i].z*.007);
      uvs.push(topUVs[i*2],topUVs[i*2+1]);
    }
    for(let i=0;i<topIndices.length;i+=3)indices.push(topIndices[i]+count,topIndices[i+2]+count,topIndices[i+1]+count);
    // Duplicate edge vertices keep the rim normals from flattening the skin.
    for(let j=0;j<rows;j++)for(const edge of [0,cols]){
      const a=j*(cols+1)+edge,b=(j+1)*(cols+1)+edge,n=positions.length/3;
      for(const index of [a,b,b+count,a+count]){positions.push(...positions.slice(index*3,index*3+3));uvs.push(...uvs.slice(index*2,index*2+2));}
      if(edge===0)indices.push(n,n+2,n+1,n,n+3,n+2);else indices.push(n,n+1,n+2,n,n+2,n+3);
    }
  }
  return geometry(positions,uvs,indices);
}

// Each blossom is an open three-dimensional corolla with a curved base, raised
// rim and scalloped petal edges. Its UV seams fall between the petal lobes.
export function flowerGeometry(species,region,detail=true){
  const level=detailLevel(detail);
  const petals=species==='gold'?8:species==='lilac'?6:7,cup=species==='cream'?.95:species==='lilac'?.80:species==='coral'?.62:.44;
  const rows=level==='inspection'?4:level==='close'?2:1,cols=level==='inspection'?3:1,positions=[],uvs=[],indices=[];
  const profile=new THREE.CubicBezierCurve3(new THREE.Vector3(),new THREE.Vector3(.36,.015,0),new THREE.Vector3(.86,cup*.27,0),new THREE.Vector3(1,cup,0));
  for(let p=0;p<petals;p++){
    const offset=positions.length/3;
    for(let j=0;j<=rows;j++){
      const t=j/rows,section=profile.getPoint(t);
      for(let i=0;i<=cols;i++){
        const u=i/cols,a=u*2-1,angle=(p+u)*Math.PI*2/petals,r=section.x*(1-.14*a*a*t*t*t),height=section.y+.09*(1-a*a)*t*t*t;
        const spread=level==='gameplay'?1.3:1;
        positions.push(Math.cos(angle)*r*spread,height,Math.sin(angle)*r*spread);uvs.push(...atlasUV(region,u,t));
        if(j<rows&&i<cols){const n=offset+j*(cols+1)+i;if(j>0)indices.push(n,n+cols+2,n+1);indices.push(n,n+cols+1,n+cols+2);}
      }
    }
  }
  return geometry(positions,uvs,indices);
}

// A tapering three-sided stem has no invisible second triangle per side.
// Its uncapped base is buried in the leaf cluster.
export function gameplayStemGeometry(){
  const positions=[],uvs=[],indices=[];
  for(let i=0;i<3;i++){
    const a=i*Math.PI*2/3,b=(i+1)*Math.PI*2/3,n=positions.length/3;
    positions.push(0,.5,0,Math.cos(b),-.5,Math.sin(b),Math.cos(a),-.5,Math.sin(a));
    uvs.push(.5,1,1,0,0,0);indices.push(n,n+1,n+2);
  }
  return geometry(positions,uvs,indices);
}

// At the held gameplay angle, the coping bevel is subpixel. Twelve quads
// retain the outside, the recessed soil opening and the continuous top rim.
export function gameplayTroughGeometry({x0,x1,z0,z1,bottom,top,soil,wall}){
  const outer=[[x0,z0],[x1,z0],[x1,z1],[x0,z1]],inner=[[x0+wall,z0+wall],[x1-wall,z0+wall],[x1-wall,z1-wall],[x0+wall,z1-wall]],positions=[],uvs=[],indices=[];
  function quad(...points){const n=positions.length/3;for(const p of points){positions.push(...p);uvs.push(0,0);}indices.push(n,n+1,n+2,n,n+2,n+3);}
  for(let i=0;i<4;i++){
    const a=outer[i],b=outer[(i+1)%4],c=inner[i],d=inner[(i+1)%4];
    quad([a[0],bottom,a[1]],[a[0],top,a[1]],[b[0],top,b[1]],[b[0],bottom,b[1]]);
    quad([c[0],top,c[1]],[c[0],soil,c[1]],[d[0],soil,d[1]],[d[0],top,d[1]]);
    quad([a[0],top,a[1]],[c[0],top,c[1]],[d[0],top,d[1]],[b[0],top,b[1]]);
  }
  return geometry(positions,uvs,indices);
}
