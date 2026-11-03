import * as THREE from '../vendor/three-0.186.1/three.module.min.js';
import {getTextureLoader} from './web-model-loader.js';
import manifest from './surface-texture-manifest.json' with {type:'json'};

const caches=new WeakMap();
const loaded=new WeakMap();
const artRoot=new URL('../art/',import.meta.url);
const pageURL=new URL('../cafe-plaza.html',import.meta.url);

export function surfaceTextureAsset(path,mode='web'){
  const url=new URL(path,globalThis.location?.href??pageURL.href);
  if(!url.href.startsWith(artRoot.href))throw new Error(`Surface texture outside civic art: ${path}`);
  const relative=url.pathname.slice(artRoot.pathname.length),[folder,name]=relative.split('/');
  const transfer=manifest[folder]?.[name];
  if(!transfer||relative.split('/').length!==2)throw new Error(`Unregistered surface texture: ${relative}`);
  if(mode!=='source')url.pathname=artRoot.pathname+folder+'/web/'+name.replace(/\.[^.]+$/,'.ktx2');
  return {url:url.href,name:`${folder}/${name}`,transfer,compressed:mode!=='source'};
}

export function loadSurfaceTexture(renderer,path){
  const params=new URLSearchParams(globalThis.location?.search??'');
  const mode=params.get('textures')==='source'||params.get('assets')==='source'?'source':'web';
  const asset=surfaceTextureAsset(path,mode);
  let cache=caches.get(renderer);
  if(!cache){cache=new Map();caches.set(renderer,cache);}
  if(!cache.has(asset.url)){
    const loader=asset.compressed?getTextureLoader(renderer):new THREE.TextureLoader();
    const pending=loader.loadAsync(asset.url).then(texture=>{
      texture.name=asset.name;
      texture.colorSpace=asset.transfer==='srgb'?THREE.SRGBColorSpace:THREE.NoColorSpace;
      texture.userData.surfaceTexture={source:asset.name,mode,url:asset.url};
      if(!loaded.has(renderer))loaded.set(renderer,new Set());
      loaded.get(renderer).add(texture);
      texture.addEventListener('dispose',()=>{
        loaded.get(renderer).delete(texture);
        if(cache.get(asset.url)===pending)cache.delete(asset.url);
      });
      return texture;
    }).catch(error=>{cache.delete(asset.url);throw error;});
    cache.set(asset.url,pending);
  }
  return cache.get(asset.url);
}

export function surfaceTextureStatistics(renderer){
  const maps=[...(loaded.get(renderer)??[])].map(texture=>{
    const {width,height}=texture.image;
    let bytes=0;
    if(texture.isCompressedTexture)bytes=texture.mipmaps.reduce((n,m)=>n+m.data.byteLength,0);
    else for(let w=width,h=height;;w=Math.max(1,w>>1),h=Math.max(1,h>>1)){
      bytes+=w*h*4;if(!texture.generateMipmaps||(w===1&&h===1))break;
    }
    return {name:texture.name,mode:texture.userData.surfaceTexture.mode,width,height,
      compressed:Boolean(texture.isCompressedTexture),format:texture.format,bytes};
  });
  return {count:maps.length,compressed:maps.filter(m=>m.compressed).length,estimatedGPUBytes:maps.reduce((n,m)=>n+m.bytes,0),maps,
    note:'Loaded standalone surface maps including sky inputs; excludes GLBs, VAT, render targets and driver overhead.'};
}
