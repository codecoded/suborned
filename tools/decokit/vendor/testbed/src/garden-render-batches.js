import * as THREE from '../vendor/three-0.186.1/three.module.min.js';
import {mergeGeometries} from '../vendor/three-0.186.1/BufferGeometryUtils.js';

// Separate bounds and material identities let the existing frustum and local
// light filters reject work per bed. Texture maps remain shared, not duplicated.
export function createGardenRenderBatches({parts,soilParts,levels,materials,spatialBatches=false}){
  const ids=spatialBatches?[...new Set(soilParts.map(g=>g.userData.bedId))]:[null];
  const batches=[];
  const select=(list,id)=>id===null?list:list.filter(g=>g.userData.bedId===id);
  const merge=list=>list.length?mergeGeometries(list):new THREE.BufferGeometry().setAttribute('position',new THREE.Float32BufferAttribute([],3));
  for(const id of ids)for(const [kind,source] of Object.entries(materials)){
    const geometry={};
    if(kind==='soil'){
      const g=merge(select(soilParts,id));for(const level of levels)geometry[level]=g;
    }else for(const level of levels)geometry[level]=merge(select(parts[level][kind],id));
    if(spatialBatches&&!geometry.gameplay.attributes.position.count){for(const g of new Set(Object.values(geometry)))g.dispose();continue;}
    batches.push({id,kind,geometry});
  }
  for(const level of levels)for(const list of Object.values(parts[level]))for(const g of list)g.dispose();
  for(const g of soilParts)g.dispose();
  return createGardenMeshes({batches,materials,spatialBatches});
}

export function createGardenMeshes({batches,materials,spatialBatches=false}){
  const materialInstances=Object.fromEntries(Object.keys(materials).map(kind=>[kind,[materials[kind]]]));
  const rendered=batches.map(({id,kind,geometry})=>{
    const source=materials[kind],material=spatialBatches?source.clone():source;
    if(spatialBatches){
      material.name=`${source.name} bed ${id}`;
      material.onBeforeCompile=source.onBeforeCompile;
      material.customProgramCacheKey=source.customProgramCacheKey;
      materialInstances[kind].push(material);
    }
    const mesh=new THREE.Mesh(geometry.gameplay,material);
    mesh.name=`Café garden ${id===null?'':id+' '}${kind}`;
    mesh.userData.plantingBed=id;mesh.castShadow=kind!=='soil';mesh.receiveShadow=true;
    return {mesh,geometry};
  });
  return {
    meshes:rendered.map(b=>b.mesh),materialInstances,
    installLevel(level,replacements){
      if(replacements.length!==batches.length||replacements.some((b,i)=>b.id!==batches[i].id||b.kind!==batches[i].kind))throw new Error('Planting detail batches do not match');
      rendered.forEach((b,i)=>b.geometry[level]=replacements[i].geometry[level]);
    },
    triangles:level=>rendered.reduce((n,{geometry})=>n+(geometry[level].index?.count??geometry[level].attributes.position.count)/3,0),
    setLOD:level=>{for(const {mesh,geometry} of rendered)mesh.geometry=geometry[level];}
  };
}
