import {GLTFLoader} from '../vendor/three-0.186.1/GLTFLoader.js';
import {KTX2Loader} from '../vendor/three-0.186.1/KTX2Loader.js';
import {MeshoptDecoder} from '../vendor/three-0.186.1/libs/meshopt_decoder.module.js';

const textureLoaders=new WeakMap();
export function getTextureLoader(renderer){
  let ktx=textureLoaders.get(renderer);
  if(!ktx){
    ktx=new KTX2Loader().setTranscoderPath(new URL('../vendor/three-0.186.1/basis/',import.meta.url).href).setWorkerLimit(2).detectSupport(renderer);
    textureLoaders.set(renderer,ktx);
  }
  return ktx;
}

export function createModelLoader(renderer){
  return new GLTFLoader().setKTX2Loader(getTextureLoader(renderer)).setMeshoptDecoder(MeshoptDecoder);
}

export function webModelURL(path){
  const url=new URL(path,location.href);
  if(new URLSearchParams(location.search).get('assets')!=='source'&&!url.pathname.includes('/web/')){
    const slash=url.pathname.lastIndexOf('/');
    url.pathname=url.pathname.slice(0,slash+1)+'web/'+url.pathname.slice(slash+1);
  }
  return url.href;
}
