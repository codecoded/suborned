import * as THREE from '../vendor/three-0.186.1/three.module.min.js';

export function gardenAtlasUV(region,u,v){return [(region%4+.012+u*.976)/4,(region<4?.5:0)+(.012+v*.976)/2];}
export function finishGardenGeometry(positions,uvs,colours,indices){
  const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(positions,3));
  g.setAttribute('uv',new THREE.Float32BufferAttribute(uvs,2));g.setAttribute('uv1',g.attributes.uv.clone());
  g.setAttribute('color',new THREE.Float32BufferAttribute(colours,3));g.setIndex(indices);g.computeVertexNormals();return g;
}

// The distant mesh retains a centre vein and a curved, rolled cross-section.
// Broad foliage uses one quiet green texture, with root occlusion in vertices.
export function gardenBladeGeometry(kind,level,region=1,{rows:rowOverride}={}){
  const strap=kind==='strap',rows=rowOverride??(level==='close'?9:level==='near'?6:strap?5:kind==='broad'?4:3),cols=2;
  const p=[],uv=[],colour=[],idx=[];
  for(let j=0;j<=rows;j++){
    const t=j/rows,breadth=Math.sin(Math.PI*t)**(strap?.75:.52),arch=Math.sin(Math.PI*t)*(strap?.27:.19);
    for(let i=0;i<=cols;i++){
      const a=i/cols*2-1;
      p.push(a*breadth*.5,t,arch-.07*a*a*breadth);
      uv.push(...gardenAtlasUV(region,i/cols,t));
      const ao=.42+.58*Math.min(1,t*2.8);colour.push(ao,ao,ao);
      if(j<rows&&i<cols){const n=j*(cols+1)+i;if(j>0)idx.push(n,n+1,n+cols+2);if(j<rows-1)idx.push(n,n+cols+2,n+cols+1);}
    }
  }
  return finishGardenGeometry(p,uv,colour,idx);
}
