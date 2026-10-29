import * as THREE from '../vendor/three-0.186.1/three.module.min.js';
import {mergeGeometries} from '../vendor/three-0.186.1/BufferGeometryUtils.js';
import {civicDistrictLook} from './civic-district-look.js';

// One chrome and opal atomic globe fixture, instanced in all four islands.
// Local light reaches the leaves and paving without extra shadow renders.
export function createFountainGardenLights(garden,centre,chromeMaterial,{placements=null,name='Fountain garden atomic globe lamps',lightIndices=null,finish='chrome'}={}){
  const group=new THREE.Group();group.name=name;
  const chrome=chromeMaterial.clone();chrome.name='Garden lamp polished chrome';
  const champagne=finish==='champagne';
  chrome.color.set(champagne?civicDistrictLook.lamp.colour:'#d8dfe4');chrome.metalness=1;
  chrome.roughness=champagne?civicDistrictLook.lamp.roughness:.16;
  if(champagne)chrome.name='Garden lamp satin champagne metal';
  const opal=new THREE.MeshStandardMaterial({name:'Garden lamp warm opal glass',color:'#fffdf0',
    emissive:'#fff1d2',emissiveIntensity:.06,roughness:.25});
  const parts=[];
  function cylinder(top,bottom,height,y){const g=new THREE.CylinderGeometry(top,bottom,height,10);g.translate(0,y,0);parts.push(g.toNonIndexed());g.dispose();}
  cylinder(.16,.19,.05,.025);cylinder(.105,.14,.075,.085);cylinder(.065,.105,.18,.21);
  cylinder(.032,.065,1.68,1.14);cylinder(.17,.05,.10,2.03);cylinder(.22,.17,.03,2.095);
  for(const y of [.32,.40,.48])cylinder(.073,.073,.025,y);
  function orbit(radius,tube,rx,rz){
    const g=new THREE.TorusGeometry(radius,tube,4,20);g.rotateX(rx);g.rotateZ(rz);g.translate(0,2.31,0);
    parts.push(g.toNonIndexed());g.dispose();
  }
  orbit(.35,.015,Math.PI/2,.26);
  orbit(.32,.012,.42,-.66);
  for(const [x,y,z] of [[.32,2.31,.10],[-.25,2.49,-.03]]){
    const g=new THREE.SphereGeometry(.041,8,5);g.translate(x,y,z);parts.push(g.toNonIndexed());g.dispose();
  }
  cylinder(.028,.052,.11,2.565);
  const chromeGeometry=mergeGeometries(parts);for(const g of parts)g.dispose();
  const globeGeometry=new THREE.SphereGeometry(.205,12,8);globeGeometry.scale(1.2,.85,1.2);globeGeometry.translate(0,2.31,0);
  const count=placements?.length??4;
  const meshes=[new THREE.InstancedMesh(chromeGeometry,chrome,count),new THREE.InstancedMesh(globeGeometry,opal,count)];
  meshes[0].name='Garden lamp chrome housings';meshes[1].name='Garden lamp opal globes';
  const sources=[],lights=[],dummy=new THREE.Object3D();
  for(let i=0;i<count;i++){
    const angle=Math.PI/4+i*Math.PI/2+.18,radius=garden?.innerRadius+.32;
    const p=placements?.[i]??{x:centre.x+Math.cos(angle)*radius,y:centre.y+Math.sin(angle)*radius};
    const base=placements?p.height: garden.bedHeight-.055;
    dummy.position.set(p.x,base,-p.y);dummy.rotation.y=i*Math.PI/2;dummy.updateMatrix();
    for(const mesh of meshes)mesh.setMatrixAt(i,dummy.matrix);
    if(!lightIndices||lightIndices.includes(i)){
      const light=new THREE.PointLight('#fff1d8',0,5.8,2);light.name='Garden globe light '+(i+1);
      light.position.set(p.x,base+2.31,-p.y);light.castShadow=false;group.add(light);lights.push(light);
    }
    sources.push({...p,height:base+2.31,bed:placements?null:garden.beds[i].id});
  }
  for(const mesh of meshes){mesh.instanceMatrix.needsUpdate=true;mesh.computeBoundingBox();mesh.computeBoundingSphere();mesh.receiveShadow=true;mesh.castShadow=true;group.add(mesh);}
  let mode='day';
  function setLighting(value){
    mode=value;const night=mode==='night',evening=mode!=='day';
    for(const light of lights)light.intensity=night?28:evening?16:0;
    opal.emissiveIntensity=night?3.5:evening?2:.06;
    chrome.envMapIntensity=night?.7:evening?.9:1.1;
  }
  function statistics(){return {instances:count,sharedFixture:true,drawBatches:2,
    triangles:meshes.reduce((n,m)=>n+(m.geometry.index?.count??m.geometry.attributes.position.count)/3*m.count,0),
    activeLights:lights.filter(l=>l.intensity>0).length,additionalShadowMaps:0,mode,sources,
    design:'Saturn opal globe, tilted atomic orbits, satellite finials, stepped chrome base',
    colour:champagne?'warm white opal, satin champagne':'warm white opal, polished chrome',intensity:lights[0].intensity,range:lights[0].distance};}
  setLighting('day');return {group,meshes,setLighting,statistics};
}
