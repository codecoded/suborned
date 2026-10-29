import * as THREE from '../vendor/three-0.186.1/three.module.min.js';
import {createGardenMeshes} from './garden-render-batches.js';
import {createGardenRuntime,gardenLevels} from './garden-runtime.js';

const version=2;
const arrayTypes={Float32Array,Uint32Array,Uint16Array,Uint8Array,Int32Array,Int16Array,Int8Array};

export function gardenCacheKey(beds,spatialBatches){
  // Includes every layout/seed parameter. Alternate previews still generate
  // their own geometry rather than accidentally reusing the plaza's planting.
  // Trigonometric layout generation can differ in the final floating-point
  // bit between engines. Nanometre-scale rounding is below authored precision.
  return JSON.stringify({version,beds,spatialBatches},(_key,value)=>typeof value==='number'?Number(value.toFixed(8)):value);
}

export function encodeGardenCache(kit,{key,sources={},levels=gardenLevels}){
  const geometryIds=new Map(),geometries=[],arrays=[];let bytes=0;
  function attribute(a){
    if(a.isInterleavedBufferAttribute)throw new Error('Bake requires plain geometry attributes');
    bytes=(bytes+3)&~3;
    const value={type:a.array.constructor.name,offset:bytes,length:a.array.length,itemSize:a.itemSize,normalized:a.normalized};
    arrays.push({offset:bytes,array:a.array});bytes+=a.array.byteLength;return value;
  }
  function geometry(g){
    if(geometryIds.has(g))return geometryIds.get(g);
    const id=geometries.length;geometryIds.set(g,id);
    geometries.push({attributes:Object.fromEntries(Object.entries(g.attributes).map(([k,a])=>[k,attribute(a)])),index:g.index?attribute(g.index):null});
    return id;
  }
  const previous=kit.lodSummary().detail,batches=kit.pickables.map(m=>({id:m.userData.plantingBed,kind:m.name.split(' ').at(-1),geometry:{}}));
  for(const level of levels){kit.setLOD(level);kit.pickables.forEach((mesh,i)=>batches[i].geometry[level]=geometry(mesh.geometry));}
  kit.setLOD(previous);
  const statistics=kit.statistics();
  const json=new TextEncoder().encode(JSON.stringify({version,key,sources,levels,statistics,batches,geometries}));
  const start=(4+json.length+3)&~3,buffer=new ArrayBuffer(start+bytes),view=new Uint8Array(buffer);
  new DataView(buffer).setUint32(0,json.length,true);view.set(json,4);
  for(const {offset,array} of arrays)view.set(new Uint8Array(array.buffer,array.byteOffset,array.byteLength),start+offset);
  return buffer;
}

function decodeGardenData(buffer,key){
  const size=new DataView(buffer).getUint32(0,true),start=(4+size+3)&~3;
  const data=JSON.parse(new TextDecoder().decode(new Uint8Array(buffer,4,size)));
  if(data.version!==version||data.key!==key)throw new Error('Planting bake does not match this layout');
  function attribute(a){
    const Type=arrayTypes[a.type];if(!Type)throw new Error('Unsupported planting attribute');
    return new THREE.BufferAttribute(new Type(buffer,start+a.offset,a.length),a.itemSize,a.normalized);
  }
  const geometries=data.geometries.map(g=>{
    const geometry=new THREE.BufferGeometry();
    for(const [k,a] of Object.entries(g.attributes))geometry.setAttribute(k,attribute(a));
    if(g.index)geometry.setIndex(attribute(g.index));
    geometry.computeBoundingBox();geometry.computeBoundingSphere();return geometry;
  });
  const batches=data.batches.map(b=>({...b,geometry:Object.fromEntries(data.levels.map(l=>[l,geometries[b.geometry[l]]]))}));
  return {data,batches};
}

export function decodeGardenCache(buffer,{key,materials,stoneMaterial,textureResolution,loadLevel,onGeometryReady}){
  const {data,batches}=decodeGardenData(buffer,key);
  for(const batch of batches)for(const level of gardenLevels)batch.geometry[level]??=batch.geometry[data.levels[0]];
  const rendering=createGardenMeshes({batches,materials:{foliage:materials.foliage,flowers:materials.petals,soil:materials.soil,stone:stoneMaterial},spatialBatches:data.statistics.spatialBatches});
  const install=loadLevel?async level=>{
    const decoded=decodeGardenData(await loadLevel(level),key);
    if(!decoded.data.levels.includes(level))throw new Error('Planting asset is missing requested detail');
    rendering.installLevel(level,decoded.batches);
  }:undefined;
  return createGardenRuntime({rendering,materials,statistics:data.statistics,textureResolution,precomputed:true,availableLevels:data.levels,loadLevel:install,onGeometryReady});
}

async function fetchCache(url){
  const response=await fetch(url);if(!response.ok)throw new Error(`Planting asset returned ${response.status}`);
  return new Response(response.body.pipeThrough(new DecompressionStream('gzip'))).arrayBuffer();
}
export async function loadGardenCache(url,options){
  try{
    return decodeGardenCache(await fetchCache(url),{...options,loadLevel:level=>fetchCache(url.replace('.bin.gz',`-${level}.bin.gz`))});
  }catch(error){console.warn('Planting bake unavailable; generating the authored layout.',error.message);return null;}
}
