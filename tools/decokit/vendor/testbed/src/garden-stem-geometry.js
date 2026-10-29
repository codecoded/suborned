import * as THREE from '../vendor/three-0.186.1/three.module.min.js';
import {gardenAtlasUV} from './garden-botanical-geometry.js';

export function gardenStemGeometry(root,crown,detail='gameplay',contains=()=>true){
  const length=root.distanceTo(crown),segments=detail==='gameplay'?3:detail==='inspection'?8:5;
  // Coordinate-derived variation leaves the seeded planting and flower packing unchanged.
  const phase=Math.sin(root.x*127.1+root.z*311.7+crown.y*74.7+crown.x*43.3)*43758.5453;
  const variation=phase-Math.floor(phase),angle=variation*Math.PI*2;
  const bend=new THREE.Vector3(Math.cos(angle),0,Math.sin(angle)).multiplyScalar(Math.min(.15,length*(.12+variation*.07)));
  const point=new THREE.Vector3();let geometry;
  for(let attempt=0;attempt<8;attempt++){
    const curve=new THREE.CubicBezierCurve3(root,root.clone().lerp(crown,.33).addScaledVector(bend,.15),crown.clone().lerp(root,.28).addScaledVector(bend,1.2),crown);
    geometry=new THREE.TubeGeometry(curve,segments,.004,3,false);
    const positions=geometry.attributes.position;
    for(let row=0;row<=segments;row++){
      const t=row/segments,center=curve.getPointAt(t);
      for(let side=0;side<=3;side++){
        const i=row*4+side;
        point.fromBufferAttribute(positions,i).sub(center).multiplyScalar(1-.25*t).add(center);
        positions.setXYZ(i,point.x,point.y,point.z);
      }
    }
    let fits=true;for(let i=0;i<positions.count;i++)if(!contains(point.fromBufferAttribute(positions,i))){fits=false;break;}
    if(fits)break;
    if(attempt===7)break;
    geometry.dispose();bend.multiplyScalar(.5);
  }
  const uv=geometry.attributes.uv;
  for(let i=0;i<uv.count;i++)uv.setXY(i,...gardenAtlasUV(1,.5+(uv.getY(i)-.5)*.3,uv.getX(i)));
  geometry.setAttribute('uv1',uv.clone());
  geometry.setAttribute('color',new THREE.Float32BufferAttribute(new Float32Array(geometry.attributes.position.count*3).fill(.46),3));
  return geometry;
}
