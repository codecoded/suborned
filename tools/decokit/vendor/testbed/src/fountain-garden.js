import * as THREE from '../vendor/three-0.186.1/three.module.min.js';
import {civicDistrictLook} from './civic-district-look.js';
import {mergeGeometries} from '../vendor/three-0.186.1/BufferGeometryUtils.js';
import {gardenAtlasUV as atlasUV,finishGardenGeometry as finish,gardenBladeGeometry as blade} from './garden-botanical-geometry.js';
import {flowerGeometry} from './cafe-botanical-geometry.js?lod=2';
import {createCivicPavingMaterial} from './civic-paving-material.js?paving=5';

const levels=['gameplay','near','close'];
const greenRegion=1;

function inside(p,vertices){let hit=false;for(let i=0,j=vertices.length-1;i<vertices.length;j=i++){
  const a=vertices[i],b=vertices[j];if((a.y>p.y)!==(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)hit=!hit;
}return hit;}
function edgeDistance(p,vertices){return Math.min(...vertices.map((a,i)=>{
  const b=vertices[(i+1)%vertices.length],dx=b.x-a.x,dy=b.y-a.y,t=THREE.MathUtils.clamp(((p.x-a.x)*dx+(p.y-a.y)*dy)/(dx*dx+dy*dy),0,1);
  return Math.hypot(p.x-a.x-t*dx,p.y-a.y-t*dy);
}));}

// Shared indexed walls have smooth normals around the curve. Narrow profile
// bands provide a coping lip and a recessed shadow line, without tile seams.
function stoneProfile(vertices,top,level){
  const normals=vertices.map((p,i)=>{
    const a=vertices[(i+vertices.length-1)%vertices.length],b=vertices[(i+1)%vertices.length];
    const u=new THREE.Vector2(p.x-a.x,p.y-a.y).normalize(),v=new THREE.Vector2(b.x-p.x,b.y-p.y).normalize();
    return new THREE.Vector2(-(u.y+v.y),u.x+v.x).divideScalar(1+u.dot(v));
  });
  const full=[[.025,0,.82],[0,.035,.90],[0,top-.12,1],[.012,top-.10,.75],
    [.012,top-.08,.75],[0,top-.06,1],[-.017,top-.043,1],[-.025,top-.023,1],
    [-.015,top-.003,1],[.01,top,1],[.115,top,1],[.14,top-.025,.86],[.14,top-.10,.70]];
  const profile=level==='gameplay'?[0,1,2,5,8,9,10,12].map(i=>full[i]):level==='near'?[0,1,2,3,4,6,8,9,10,12].map(i=>full[i]):full;
  const p=[],uv=[],colours=[],indices=[],n=vertices.length;
  for(const [offset,h,tint] of profile)for(let i=0;i<n;i++){
    const v=vertices[i],normal=normals[i],x=v.x+normal.x*offset,y=v.y+normal.y*offset;
    p.push(x,h,-y);uv.push(x/3,h/3);colours.push(tint,tint,tint);
  }
  for(let j=0;j<profile.length-1;j++)for(let i=0;i<n;i++){
    const a=j*n+i,b=j*n+(i+1)%n,c=(j+1)*n+(i+1)%n,d=(j+1)*n+i;
    indices.push(a,b,c,a,c,d);
  }
  return finish(p,uv,colours,indices);
}

export function createFountainGarden(garden,centre,plantMaterials,stoneSource){
  const group=new THREE.Group();group.name='Sculpted fountain tropical garden';
  const prototype=garden.beds[0],vertices=prototype.vertices,soil=prototype.z[1]-.055;
  const foliage=new THREE.MeshStandardMaterial({name:'Fountain dark glossy tropical foliage',
    map:plantMaterials.foliage.map,normalMap:plantMaterials.foliage.normalMap,roughnessMap:plantMaterials.foliage.roughnessMap,
    color:civicDistrictLook.foliage,vertexColors:true,normalScale:new THREE.Vector2(.16,.16),roughness:.49,side:THREE.DoubleSide});
  const petals=plantMaterials.petals.clone();petals.name='Fountain clustered coral and amber flowers';petals.vertexColors=true;petals.roughness=.64;petals.normalScale.set(.16,.16);
  const stone=createCivicPavingMaterial(stoneSource,{lift:.20,neutrality:.96,roughness:.50,reflectionStrength:.55});
  stone.name='Fountain garden pale sculpted stone';stone.vertexColors=true;
  const soilMaterial=plantMaterials.soil;
  const parts=Object.fromEntries(levels.map(l=>[l,{foliage:[],flowers:[]}]))
  const templates=Object.fromEntries(levels.map(l=>[l,Object.fromEntries(['small','broad','strap'].map(k=>[k,blade(k,l)]))]));
  const up=new THREE.Vector3(0,1,0);let seed=21761,leafCount=0,flowerCount=0;
  const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
  const local=(p,h)=>new THREE.Vector3(p.x-centre.x,h,-p.y+centre.y);
  function addLeaf(kind,root,tip,width,tint){
    const start=local(root,root.h),end=local(tip,tip.h),dir=end.sub(start),length=dir.length(),y=dir.normalize();
    const x=new THREE.Vector3().crossVectors(y,up).normalize();if(x.lengthSq()<.001)x.set(1,0,0);
    const z=new THREE.Vector3().crossVectors(x,y).normalize(),matrix=new THREE.Matrix4().makeBasis(x.multiplyScalar(width),y.multiplyScalar(length),z.multiplyScalar(length));matrix.setPosition(start);
    // Keep the entire blade on the garden footprint, including every LOD.
    let fraction=1;const v=new THREE.Vector3(),detail=templates.close[kind];
    for(let i=0;i<detail.attributes.position.count;i++){
      v.fromBufferAttribute(detail.attributes.position,i).applyMatrix4(matrix);
      const p={x:v.x+centre.x,y:centre.y-v.z};if(inside(p,vertices))continue;
      let a=0,b=1;for(let j=0;j<10;j++){const t=(a+b)/2,q={x:root.x+(p.x-root.x)*t,y:root.y+(p.y-root.y)*t};if(inside(q,vertices))a=t;else b=t;}
      fraction=Math.min(fraction,a);
    }
    for(const i of [0,4,8,2,6,10])matrix.elements[i]*=fraction;
    const c=new THREE.Color(tint);
    for(const l of levels){const g=templates[l][kind].clone().applyMatrix4(matrix),colour=g.attributes.color;
      for(let i=0;i<colour.count;i++)colour.setXYZ(i,colour.getX(i)*c.r,colour.getY(i)*c.g,colour.getZ(i)*c.b);
      parts[l].foliage.push(g.toNonIndexed());g.dispose();}
    leafCount++;
  }
  function leafClump(p,height,scale=1){
    const phase=random()*Math.PI*2;
    for(let i=0;i<6;i++){
      const a=phase+i*2.39996,span=(.24+random()*.12)*scale;
      addLeaf('small',{...p,h:soil+.025},{x:p.x+Math.cos(a)*span,y:p.y+Math.sin(a)*span,h:soil+height*(.75+random()*.4)},(.26+random()*.09)*scale,
        ['#c2cdb0','#b0c8b0','#e1ddac'][i%3]);
    }
  }
  // Rounded dark leaf clumps fill the entire soil opening. Repetition within
  // a restrained palette creates a mass, rather than multicoloured confetti.
  for(let x=prototype.x[0]+.12;x<prototype.x[1];x+=.43)for(let y=prototype.y[0]+.12;y<prototype.y[1];y+=.43){
    const p={x:x+(random()-.5)*.07,y:y+(random()-.5)*.07};
    if(!inside(p,vertices)||edgeDistance(p,vertices)<.18)continue;
    leafClump(p,.14+random()*.12,edgeDistance(p,vertices)<.32?.85:1.15);
  }
  const polar=(r,a)=>({x:centre.x+Math.cos(a)*r,y:centre.y+Math.sin(a)*r});
  // Two high arching rosettes and a lower middle fan give the bed hierarchy.
  for(const [j,offset] of [-.17,0,.17].entries()){
    const p=polar(6.2,Math.PI/4+offset),phase=random()*Math.PI*2;
    for(let i=0;i<9;i++){
      const a=phase+i*2.39996,span=.34+random()*.25;
      addLeaf('strap',{...p,h:soil+.035},{x:p.x+Math.cos(a)*span,y:p.y+Math.sin(a)*span,h:soil+(j===1?.30:.51)+random()*.18},.12+random()*.08,
        ['#acbeb4','#8fae9b','#d0ceb0'][i%3]);
    }
    for(let i=0;i<4;i++){
      const a=phase+i*Math.PI/2,span=.30+random()*.10;
      addLeaf('broad',{...p,h:soil+.04},{x:p.x+Math.cos(a)*span,y:p.y+Math.sin(a)*span,h:soil+.20+random()*.09},.25+random()*.09,'#a9ba94');
    }
  }
  for(const [j,offset] of [-.14,.03,.18].entries()){
    const p=polar(6.2+(j===1?.24:-.21),Math.PI/4+offset),phase=random()*Math.PI*2;
    for(let i=0;i<9;i++){
      const a=phase+i*2.39996,spread=.07+.16*Math.sqrt(i/9),h=soil+.38+random()*.24;
      const crown=local({x:p.x+Math.cos(a)*spread,y:p.y+Math.sin(a)*spread},h),species=j===1?'gold':'coral',region=j===1?5:4;
      const orientation=new THREE.Quaternion().setFromEuler(new THREE.Euler((random()-.5)*.7,a,(random()-.5)*.6)),radius=.13+random()*.030;
      for(const l of levels){
        const start=local(p,soil+.035),delta=crown.clone().sub(start),stem=new THREE.CylinderGeometry(.004,.006,delta.length(),4,1,true);
        stem.applyMatrix4(new THREE.Matrix4().compose(start.add(crown).multiplyScalar(.5),new THREE.Quaternion().setFromUnitVectors(up,delta.clone().normalize()),new THREE.Vector3(1,1,1)));
        stem.setAttribute('color',new THREE.Float32BufferAttribute(new Float32Array(stem.attributes.position.count*3).fill(.52),3));
        for(let k=0;k<stem.attributes.uv.count;k++)stem.attributes.uv.setXY(k,...atlasUV(greenRegion,.5,k%2));stem.setAttribute('uv1',stem.attributes.uv.clone());
        parts[l].foliage.push(stem.toNonIndexed());stem.dispose();
        const g=flowerGeometry(species,region,l==='gameplay'?'close':l==='near'?'close':'inspection');
        g.applyMatrix4(new THREE.Matrix4().compose(crown,orientation,new THREE.Vector3(radius,radius,radius)));
        const colour=new Float32Array(g.attributes.position.count*3);for(let k=0;k<colour.length;k+=3){colour[k]=1;colour[k+1]=j===1?.85:.76;colour[k+2]=j===1?.50:.49;}
        g.setAttribute('color',new THREE.Float32BufferAttribute(colour,3));parts[l].flowers.push(g.toNonIndexed());g.dispose();
      }
      flowerCount++;
    }
  }
  const stoneGeometries=Object.fromEntries(levels.map(l=>{const g=stoneProfile(vertices,prototype.z[1],l);g.translate(-centre.x,0,centre.y);return [l,g];}));
  const soilShape=new THREE.Shape(vertices.map(p=>new THREE.Vector2(p.x-centre.x,p.y-centre.y))),soilGeometry=new THREE.ShapeGeometry(soilShape);
  soilGeometry.rotateX(-Math.PI/2);soilGeometry.translate(0,soil-.018,0);soilGeometry.setAttribute('uv1',soilGeometry.attributes.uv.clone());
  const geometry={stone:stoneGeometries,soil:Object.fromEntries(levels.map(l=>[l,soilGeometry])),foliage:{},flowers:{}};
  for(const l of levels)for(const kind of ['foliage','flowers']){
    geometry[kind][l]=mergeGeometries(parts[l][kind]);for(const g of parts[l][kind])g.dispose();
  }
  for(const l of levels)for(const g of Object.values(templates[l]))g.dispose();
  const materials={stone,soil:soilMaterial,foliage,flowers:petals},meshes=[];let detail='gameplay',projectedLeafPixels=0;
  for(const [kind,material] of Object.entries(materials)){
    const mesh=new THREE.InstancedMesh(geometry[kind][detail],material,4);mesh.name='Fountain garden '+kind;
    for(let i=0;i<4;i++){
      const m=new THREE.Matrix4().makeTranslation(centre.x,0,-centre.y).multiply(new THREE.Matrix4().makeRotationY(i*Math.PI/2));mesh.setMatrixAt(i,m);
    }
    mesh.instanceMatrix.needsUpdate=true;mesh.computeBoundingBox();mesh.computeBoundingSphere();mesh.castShadow=kind!=='soil';mesh.receiveShadow=true;group.add(mesh);meshes.push(mesh);
  }
  function updateLOD({worldWidth,viewportWidth,override='auto'}){
    projectedLeafPixels=.55*viewportWidth/worldWidth;
    const next=override==='auto'?(projectedLeafPixels>=44?'close':projectedLeafPixels>=24?'near':'gameplay'):override==='inspection'?'close':override;
    if(next===detail)return false;detail=next;for(const mesh of meshes){mesh.geometry=geometry[mesh.name.split(' ').at(-1)][detail];mesh.computeBoundingBox();mesh.computeBoundingSphere();}return true;
  }
  function setLighting(mode){foliage.envMapIntensity=mode==='night'?.28:mode==='day'?.55:.42;petals.envMapIntensity=mode==='night'?.22:.4;}
  function setWet(on){foliage.roughness=on?.34:.49;petals.roughness=on?.46:.64;}
  const triangles=l=>Object.values(geometry).reduce((n,g)=>n+(g[l].index?.count??g[l].attributes.position.count)/3*4,0);
  function statistics(){return {prototypeInstances:4,drawBatches:4,materials:4,detail,triangles:triangles(detail),lod:Object.fromEntries(levels.map(l=>[l,triangles(l)])),
    leavesPerBed:leafCount,flowersPerBed:flowerCount,projectedLeafPixels,newTextures:0,opaque:meshes.every(m=>!m.material.transparent),
    design:'Rounded glossy leaf clumps, arching tropical rosettes, three clustered coral/amber flower accents, profiled smooth pale stone',
    sameSilhouetteAtEveryLOD:true,sharedMapsWithCafe:true};}
  return {group,meshes,pickables:meshes,updateLOD,setLighting,setWet,statistics};
}
