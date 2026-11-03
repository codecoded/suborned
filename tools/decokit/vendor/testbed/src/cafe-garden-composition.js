import * as THREE from '../vendor/three-0.186.1/three.module.min.js';
import {mergeGeometries} from '../vendor/three-0.186.1/BufferGeometryUtils.js';
import {gardenBladeGeometry} from './garden-botanical-geometry.js';
import {flowerGeometry} from './cafe-botanical-geometry.js?lod=2';
import {bedGeometry} from './cafe-bed-geometry.js';
import {gardenStemGeometry} from './garden-stem-geometry.js';
import {createGardenRenderBatches} from './garden-render-batches.js';
import {createGardenRuntime} from './garden-runtime.js';

const levels=['gameplay','near','close','inspection'];
const schemes={
  'bed-west':{name:'Coral and cream, with amber accents',palettes:[['coral','cream'],['gold','coral'],['cream','lilac']]},
  'bed-east':{name:'Lilac and gold, with coral near the steps',palettes:[['coral','cream'],['lilac','gold'],['gold','cream']]},
  'bed-side':{name:'Cream and lilac, punctuated by coral',palettes:[['cream','lilac'],['coral','gold'],['lilac','cream']]}
};
const regions={coral:4,gold:5,lilac:6,cream:7};
const leafTints=['#b6c7ac','#93ada3','#c8d0a8','#acc7b4'];
function seedFor(id){return Array.from(id).reduce((n,c)=>Math.imul(n^c.charCodeAt(0),16777619)>>>0,2166136261);}
function inside(p,vertices){let hit=false;for(let i=0,j=vertices.length-1;i<vertices.length;j=i++){
  const a=vertices[i],b=vertices[j];if((a.y>p.y)!==(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)hit=!hit;
}return hit;}
function edgeDistance(p,vertices){return Math.min(...vertices.map((a,i)=>{
  const b=vertices[(i+1)%vertices.length],dx=b.x-a.x,dy=b.y-a.y,t=THREE.MathUtils.clamp(((p.x-a.x)*dx+(p.y-a.y)*dy)/(dx*dx+dy*dy),0,1);
  return Math.hypot(p.x-a.x-t*dx,p.y-a.y-t*dy);
}));}
function stoneUV(g){const p=g.attributes.position,n=g.attributes.normal,uv=[];for(let i=0;i<p.count;i++){
  const x=Math.abs(n.getX(i)),y=Math.abs(n.getY(i)),z=Math.abs(n.getZ(i));
  uv.push(...(y>=x&&y>=z?[p.getX(i)/2,-p.getZ(i)/2]:x>z?[-p.getZ(i)/2,p.getY(i)/2]:[p.getX(i)/2,p.getY(i)/2]));
}g.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));g.setAttribute('uv1',g.attributes.uv.clone());return g;}

// Compose the held strips and root bays independently. Irregular colour drifts
// and height waves are authored at load time, with no per-frame plant work.
export function createCafeGardenComposition({beds,materials,stoneMaterial,textureResolution,gameplayLeafTransform,leafGeometryFactory=gardenBladeGeometry,windAttributes=false,spatialBatches=false}){
  const parts=Object.fromEntries(levels.map(l=>[l,{foliage:[],flowers:[],stone:[]}]))
  const soilParts=[],evidence=[],leafSurfaces=[],pendingBlooms=[],up=new THREE.Vector3(0,1,0),point=new THREE.Vector3();
  const templates=Object.fromEntries(levels.map(l=>[l,Object.fromEntries(['small','broad','strap'].map(k=>[k,leafGeometryFactory(k,l==='inspection'?'close':l,1,l==='gameplay'?{rows:k==='small'?2:3}:{})]))]));
  const bloomTemplates=Object.fromEntries(Object.entries(regions).map(([species,region])=>[species,
    ['gameplay','near','inspection'].map(l=>flowerGeometry(species,region,l==='gameplay'?'near':l==='near'?'close':'inspection'))]));
  const bloomBounds=[],bloomClearance=.012;
  const leafCells=new Map(),leafCellSize=1;
  const pos=(p,h)=>new THREE.Vector3(p.x,h,-p.y);let leafCount=0,flowerCount=0,clippedLeaves=0,footprintViolations=0,rejectedBloomPlacements=0,rejectedLeafConflicts=0;
  const triangle=new THREE.Triangle(),closestPoint=new THREE.Vector3(),clearanceSphere=new THREE.Sphere();
  function surfaceConflict({geometry,bounds},sphere){
    clearanceSphere.copy(sphere);clearanceSphere.radius+=bloomClearance;
    if(!bounds.intersectsSphere(clearanceSphere))return false;
    const p=geometry.attributes.position,index=geometry.index;
    for(let i=0;i<(index?.count??p.count);i+=3){
      triangle.a.fromBufferAttribute(p,index?index.getX(i):i);triangle.b.fromBufferAttribute(p,index?index.getX(i+1):i+1);triangle.c.fromBufferAttribute(p,index?index.getX(i+2):i+2);
      triangle.closestPointToPoint(sphere.center,closestPoint);
      if(closestPoint.distanceTo(sphere.center)<clearanceSphere.radius)return true;
    }return false;
  }
  function leafConflict(sphere){
    const radius=sphere.radius+bloomClearance,seen=new Set();
    for(let x=Math.floor((sphere.center.x-radius)/leafCellSize);x<=Math.floor((sphere.center.x+radius)/leafCellSize);x++)
      for(let z=Math.floor((sphere.center.z-radius)/leafCellSize);z<=Math.floor((sphere.center.z+radius)/leafCellSize);z++)
        for(const surface of leafCells.get(`${x},${z}`)??[]){
          if(seen.has(surface))continue;seen.add(surface);
          if(surfaceConflict(surface,sphere))return true;
        }
    return false;
  }
  function fitGeometry(g,root,bed,record=true){
    let fraction=1;const p=g.attributes.position;
    for(let i=0;i<p.count;i++){
      point.fromBufferAttribute(p,i);const target={x:point.x,y:-point.z};if(inside(target,bed.vertices))continue;
      let lo=0,hi=1;for(let j=0;j<12;j++){const t=(lo+hi)/2,q={x:root.x+(target.x-root.x)*t,y:root.y+(target.y-root.y)*t};if(inside(q,bed.vertices))lo=t;else hi=t;}
      fraction=Math.min(fraction,lo);
    }
    if(fraction<1){if(record)clippedLeaves++;fraction*=.995;for(let i=0;i<p.count;i++)p.setXYZ(i,root.x+(p.getX(i)-root.x)*fraction,p.getY(i),-root.y+(p.getZ(i)+root.y)*fraction);g.computeVertexNormals();}
    // Shrinking toward a root can enter a concave trough's notch even when a
    // vertex began inside. Recheck the complete silhouette after that fit.
    const outside=()=>{
      for(let i=0;i<p.count;i++)if(!inside({x:p.getX(i),y:-p.getZ(i)},bed.vertices)&&edgeDistance({x:p.getX(i),y:-p.getZ(i)},bed.vertices)>.0001)return true;
      return false;
    };
    let refitted=false;
    for(let attempt=0;attempt<24&&outside();attempt++){
      for(let i=0;i<p.count;i++)p.setXYZ(i,root.x+(p.getX(i)-root.x)*.85,p.getY(i),-root.y+(p.getZ(i)+root.y)*.85);
      refitted=true;
    }
    if(refitted){g.computeVertexNormals();if(record&&fraction===1)clippedLeaves++;}
    if(record)for(let i=0;i<p.count;i++){const q={x:p.getX(i),y:-p.getZ(i)};if(!inside(q,bed.vertices)&&edgeDistance(q,bed.vertices)>.0001)footprintViolations++;}
    return g;
  }
  function reserveBloom(species,crown,radius,rotation,bed){
    if(!inside(crown,bed.vertices))return false;
    const matrix=new THREE.Matrix4().compose(pos(crown,crown.h),rotation,new THREE.Vector3(radius,radius,radius));let sphere=null;
    // A union of the actual fitted bloom bounds at every LOD prevents two
    // crowns cutting through each other when detail or camera angle changes.
    for(const template of bloomTemplates[species]){
      const g=fitGeometry(template.clone().applyMatrix4(matrix),crown,bed,false);g.computeBoundingSphere();const b=g.boundingSphere; 
      if(!sphere)sphere=b.clone();
      else{
        const delta=b.center.clone().sub(sphere.center),distance=delta.length();
        if(b.radius>=distance+sphere.radius)sphere.copy(b);
        else if(sphere.radius<distance+b.radius){const radius=(distance+sphere.radius+b.radius)/2;if(distance>0)sphere.center.addScaledVector(delta,(radius-sphere.radius)/distance);sphere.radius=radius;}
      }g.dispose();
    }
    if(bloomBounds.some(b=>sphere.center.distanceTo(b.sphere.center)<sphere.radius+b.sphere.radius+bloomClearance)){rejectedBloomPlacements++;return false;}
    if(leafConflict(sphere)){rejectedLeafConflicts++;return false;}
    bloomBounds.push({species,bed:bed.id,sphere});return true;
  }
  function addLeaf(kind,root,tip,width,tint,bed){
    const start=pos(root,root.h),direction=pos(tip,tip.h).sub(start),length=direction.length(),y=direction.normalize();
    const x=new THREE.Vector3().crossVectors(y,up).normalize();if(x.lengthSq()<.001)x.set(1,0,0);
    const z=new THREE.Vector3().crossVectors(x,y).normalize(),m=new THREE.Matrix4().makeBasis(x.multiplyScalar(width),y.multiplyScalar(length),z.multiplyScalar(length));m.setPosition(start);
    const colour=new THREE.Color(tint);
    for(const l of levels){const g=fitGeometry(templates[l][kind].clone().applyMatrix4(m),root,bed),c=g.attributes.color;
      for(let i=0;i<c.count;i++)c.setXYZ(i,c.getX(i)*colour.r,c.getY(i)*colour.g,c.getZ(i)*colour.b);
      addWindData(g,root,tip.h);
      const baked=g.toNonIndexed();baked.userData.leafKind=kind;baked.userData.bedId=bed.id;parts[l].foliage.push(baked);
      if(l!=='close'){
        baked.computeBoundingBox();const surface={geometry:baked,bounds:baked.boundingBox.clone()};leafSurfaces.push(surface);
        // All intersecting bounds enter the grid; blossom clearance remains an
        // exact triangle test, including adjacent beds and every held detail.
        const b=surface.bounds;
        for(let x=Math.floor(b.min.x/leafCellSize);x<=Math.floor(b.max.x/leafCellSize);x++)
          for(let z=Math.floor(b.min.z/leafCellSize);z<=Math.floor(b.max.z/leafCellSize);z++){
            const key=`${x},${z}`;if(!leafCells.has(key))leafCells.set(key,[]);leafCells.get(key).push(surface);
          }
      }g.dispose();}
    leafCount++;
  }
  function addWindData(g,root,height,head=false){
    if(!windAttributes)return;
    const p=g.attributes.position,roots=new Float32Array(p.count*3),weights=new Float32Array(p.count);
    for(let i=0;i<p.count;i++){
      roots.set([root.x,root.h,-root.y],i*3);
      const t=THREE.MathUtils.clamp((p.getY(i)-root.h)/Math.max(.001,height-root.h),0,1);
      weights[i]=head?1:t*t*(3-2*t);
    }
    g.setAttribute('windRoot',new THREE.Float32BufferAttribute(roots,3));
    g.setAttribute('windWeight',new THREE.Float32BufferAttribute(weights,1));
  }
  function addFlower(species,root,crown,radius,rotation,bed){
    for(const l of levels){
      const g=flowerGeometry(species,regions[species],l==='inspection'?'inspection':l==='gameplay'?'near':'close');
      g.applyMatrix4(new THREE.Matrix4().compose(pos(crown,crown.h),rotation,new THREE.Vector3(radius,radius,radius)));
      fitGeometry(g,crown,bed);const col=new Float32Array(g.attributes.position.count*3).fill(1);
      g.setAttribute('color',new THREE.Float32BufferAttribute(col,3));addWindData(g,root,crown.h,true);const bloom=g.toNonIndexed();bloom.userData.bedId=bed.id;parts[l].flowers.push(bloom);g.dispose();
      const stem=gardenStemGeometry(pos(root,root.h),pos(crown,crown.h),l,p=>inside({x:p.x,y:-p.z},bed.vertices));
      fitGeometry(stem,root,bed);
      addWindData(stem,root,crown.h);
      const bakedStem=stem.toNonIndexed();bakedStem.userData.bedId=bed.id;parts[l].foliage.push(bakedStem);stem.dispose();
    }flowerCount++;
  }
  for(const bed of beds){
    const polygon=bed.vertices??[{x:bed.x[0],y:bed.y[0]},{x:bed.x[1],y:bed.y[0]},{x:bed.x[1],y:bed.y[1]},{x:bed.x[0],y:bed.y[1]}];
    const footprint={...bed,vertices:polygon},soil=bed.z[1]-.055,shell=bedGeometry(bed);
    const detailed=mergeGeometries([stoneUV(shell.body),stoneUV(shell.rim)]);shell.body.dispose();shell.rim.dispose();
    for(const l of levels){const g=l==='gameplay'?stoneUV(shell.gameplay):detailed.clone();g.userData.bedId=bed.id;parts[l].stone.push(g);}
    detailed.dispose();const soilGeometry=stoneUV(shell.soil).toNonIndexed();soilGeometry.userData.bedId=bed.id;soilParts.push(soilGeometry);shell.soil.dispose();
    const lush=bed.plantingProfile==='lush-tropical';
    const scheme=bed.palette??schemes[bed.id]??schemes['bed-west'],bedEvidence={id:bed.id,module:bed.module,plantingProfile:bed.plantingProfile,palette:scheme.name,seed:bed.seed??seedFor(bed.id),clusters:[],soilHeight:soil};
    for(const patch of bed.patches??[bed]){
      let seed=patch.seed??bed.seed??seedFor(patch.id);const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
      const w=patch.x[1]-patch.x[0],d=patch.y[1]-patch.y[0],horizontal=w>=d,length=Math.max(w,d),half=Math.min(w,d)/2;
      const cx=(patch.x[0]+patch.x[1])/2,cy=(patch.y[0]+patch.y[1])/2,count=patch.plantingPoints?.length??Math.max(1,Math.ceil((length-.08)/.38)),rootBay=patch.id.includes('-root-');
      const weights=Array.from({length:count},()=>.7+random()*.6),total=weights.reduce((a,b)=>a+b,0);let distance=0;
      const at=(along,cross,h)=>({x:cx+(horizontal?along:cross),y:cy+(horizontal?cross:along),h});
      for(let c=0;c<count;c++){
        const along=-length/2+.06+(length-.12)*(distance+weights[c]/2)/total;distance+=weights[c];
        const root=at(along,(random()-.5)*Math.min(.12,half*.6),soil+.022),anchor=patch.plantingPoints?.[c];
        if(anchor){const cross=(random()-.5)*Math.min(lush?.045:.12,patch.width*.15);root.x=anchor.x-Math.sin(anchor.heading)*cross;root.y=anchor.y+Math.cos(anchor.heading)*cross;}
        const canopy=anchor?.layer==='canopy',phase=random()*Math.PI*2,height=lush?.23+random()*.16:.11+random()*.15;
        for(let i=0;i<(lush?7:5);i++){
          const angle=phase+i*2.39996,span=lush?.30+random()*.16:.19+random()*.12;
          addLeaf('small',root,{x:root.x+Math.cos(angle)*span,y:root.y+Math.sin(angle)*span,h:soil+height*(.8+random()*.35)},lush?.27+random()*.14:.20+random()*.12,leafTints[(c+i+Math.floor(random()*3))%4],footprint);
        }
        if(lush)for(let i=0;i<(canopy?4:3);i++){
          const angle=phase+i*2.39996,span=.26+random()*.18;
          addLeaf('broad',root,{x:root.x+Math.cos(angle)*span,y:root.y+Math.sin(angle)*span,h:soil+(canopy?.48:.36)+random()*.19},.29+random()*.15,leafTints[(c+i)%4],footprint);
        }
        const accent=bed.plantingAccent??{};
        const t=count===1?random():c/(count-1),zone=Math.min(2,Math.floor(t*3)),fan=random()<Math.min(1,(lush?(canopy?.85:.20):rootBay?.30:.24)*(accent.fanDensity??1)),flower=random()<Math.min(1,(lush?(canopy?.72:.35):rootBay?.48:.62)*(accent.flowerDensity??1));
        if(fan){
          const fanHeight=lush?(canopy?.72:.52)+random()*.32:(rootBay?.38:.29)+random()*.20;
          for(let i=0;i<(lush?7:5);i++){
            const a=phase+i*2.39996,span=.20+random()*.14;
            addLeaf('strap',root,{x:root.x+Math.cos(a)*span,y:root.y+Math.sin(a)*span,h:soil+fanHeight*(.8+random()*.3)},lush?.12+random()*.07:.08+random()*.045,leafTints[(i+zone)%4],footprint);
          }
        }
        let primary=null;const cluster={patch:patch.id,x:root.x,y:root.y,leafHeight:height,fan,flower:null,flowerHeads:0};bedEvidence.clusters.push(cluster);
        if(flower){
          const palette=scheme.palettes[zone];primary=palette[Math.floor(random()*palette.length)];const requestedHeads=(lush?2:3)+Math.floor(random()*2);
          cluster.flower=primary;const flowerHeight=lush?(canopy?1.02:.75)+random()*.20:.30+random()*.12;
          for(let i=0;i<requestedHeads;i++){
            const species=random()<.18?palette[1]:primary,radius=.075+random()*.029,candidates=[];
            for(let attempt=0;attempt<20;attempt++){
              const a=phase+i*2.39996+attempt*.73,spread=.07+random()*.11;
              const crown={x:root.x+Math.cos(a)*spread,y:root.y+Math.sin(a)*spread,h:soil+flowerHeight+(random()-.25)*.15};
              const rotation=new THREE.Quaternion().setFromEuler(new THREE.Euler((random()-.5)*.5,a,(random()-.5)*.45));
              candidates.push({crown,rotation});
            }
            pendingBlooms.push({species,root,radius,footprint,candidates,cluster});
          }
        }
      }
    }evidence.push(bedEvidence);
  }
  // Finish all leaves before packing blossoms, so later foliage cannot grow
  // through an already placed cup. Stems may pass through their own rosette.
  for(const {species,root,radius,footprint,candidates,cluster} of pendingBlooms)for(const {crown,rotation} of candidates){
    if(!reserveBloom(species,crown,radius,rotation,footprint))continue;
    addFlower(species,root,crown,radius,rotation,footprint);cluster.flowerHeads++;break;
  }
  let bloomLeafIntersections=0;for(const {sphere} of bloomBounds)bloomLeafIntersections+=Number(leafConflict(sphere));
  if(bloomLeafIntersections)throw new Error(`${bloomLeafIntersections} café blooms intersect foliage`);
  const gameplayOptimization={optimizedLeaves:0,protectedLeaves:0,trianglesSaved:0};
  if(gameplayLeafTransform)for(let i=0;i<parts.gameplay.foliage.length;i++){
    const source=parts.gameplay.foliage[i],kind=source.userData.leafKind;if(!kind)continue;
    const result=gameplayLeafTransform(source,kind);if(!result||result===source)continue;
    result.computeBoundingBox();
    // Pack flowers against the original leaves, then simplify only where the
    // replacement preserves that clearance. No random sequence or bloom moves.
    if(bloomBounds.some(({sphere})=>surfaceConflict({geometry:result,bounds:result.boundingBox},sphere))){result.dispose();gameplayOptimization.protectedLeaves++;continue;}
    const baked=result.index?result.toNonIndexed():result;
    baked.userData.bedId=source.userData.bedId;
    gameplayOptimization.optimizedLeaves++;gameplayOptimization.trianglesSaved+=(source.attributes.position.count-baked.attributes.position.count)/3;
    parts.gameplay.foliage[i]=baked;if(baked!==result)result.dispose();source.dispose();
  }
  leafSurfaces.length=pendingBlooms.length=0;
  leafCells.clear();
  if(footprintViolations)throw new Error(`${footprintViolations} café garden vertices exceed their held planting footprint`);
  for(const l of levels)for(const g of Object.values(templates[l]))g.dispose();
  for(const list of Object.values(bloomTemplates))for(const g of list)g.dispose();
  const rendering=createGardenRenderBatches({parts,soilParts,levels,materials:{foliage:materials.foliage,flowers:materials.petals,stone:stoneMaterial,soil:materials.soil},spatialBatches});
  let minimumBloomClearance=Infinity,bloomIntersections=0;
  for(let i=0;i<bloomBounds.length;i++)for(let j=0;j<i;j++){
    const a=bloomBounds[i].sphere,b=bloomBounds[j].sphere,gap=a.center.distanceTo(b.center)-a.radius-b.radius;
    minimumBloomClearance=Math.min(minimumBloomClearance,gap);bloomIntersections+=Number(gap<0);
  }
  const statistics={gameplayOptimization,beds:evidence,treeBays:beds.filter(b=>b.treeBay).map(b=>({id:b.id,...b.treeBay,continuousSoil:true})),
    spatialBatches,leafCount,flowerHeads:flowerCount,textureResolution,textureSource:'Original imagegen botanical atlas v2; quiet green foliage region',
    composition:'Independent seeded colour drifts, uneven clump spacing, mixed foliage heights and scattered tropical fans; flower colours grouped within clumps',
    opaque:true,curvature:'Rolled curved leaves, varied bowed stems and raised cupped flowers at every LOD',footprintViolations,fittedGeometryVariants:clippedLeaves,wetSupported:true,newTextures:0,
    lodSelection:'Projected 0.4 metre leaf span; hysteresis at 16/18, 28/32 and 60/72 pixels',
    bloomIntersections,minimumBloomClearance:Number.isFinite(minimumBloomClearance)?minimumBloomClearance:null,rejectedBloomPlacements,bloomBoundsCoverAllLODs:true,
    bloomLeafIntersections,rejectedLeafConflicts};
  return createGardenRuntime({rendering,materials,statistics,textureResolution});
}
