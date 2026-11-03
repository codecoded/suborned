import * as THREE from '../vendor/three-0.186.1/three.module.min.js';

// Mitred inward offsets retain the wall thickness around courtyard corners
// and the fountain's curved, polygon-sampled beds.
export function bedGeometry(bed){
  const points=bed.vertices??[
    {x:bed.x[0],y:bed.y[0]},{x:bed.x[1],y:bed.y[0]},
    {x:bed.x[1],y:bed.y[1]},{x:bed.x[0],y:bed.y[1]}];
  const wall=bed.wall??.055,inner=points.map((p,i)=>{
    const prev=points[(i+points.length-1)%points.length],next=points[(i+1)%points.length];
    const a=new THREE.Vector2(p.x-prev.x,p.y-prev.y).normalize(),b=new THREE.Vector2(next.x-p.x,next.y-p.y).normalize();
    const scale=wall/Math.max(.1,1+a.dot(b));
    return new THREE.Vector2(p.x-scale*(a.y+b.y),p.y+scale*(a.x+b.x));
  });
  const outline=()=>new THREE.Shape(points.map(p=>new THREE.Vector2(p.x,p.y)));
  function shell(depth,bevelEnabled=false){
    const s=outline();s.holes.push(new THREE.Path(inner.slice().reverse()));
    const g=new THREE.ExtrudeGeometry(s,{depth,bevelEnabled,bevelSize:.006,bevelThickness:.005,bevelSegments:2,steps:1});
    g.rotateX(-Math.PI/2);return g;
  }
  const [bottom,top]=bed.z,body=shell(top-bottom-.035);body.translate(0,bottom,0);
  const rim=shell(.025,true);rim.translate(0,top-.030,0);
  const soil=new THREE.ShapeGeometry(new THREE.Shape(inner));soil.rotateX(-Math.PI/2);soil.translate(0,top-.055,0);
  const gameplay=shell(top-bottom);gameplay.translate(0,bottom,0);
  return {body,rim,soil,gameplay};
}
