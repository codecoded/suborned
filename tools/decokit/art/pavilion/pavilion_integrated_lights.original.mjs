import fs from 'node:fs/promises';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
import * as THREE from '../vendor/three.module.min.js';

// Append fittings without remeshing the approved body or rebaking its artwork.
// Usage: node pavilion_integrated_lights.mjs <gltf-transform node_modules>
const deps=process.argv[2];
if(!deps)throw new Error('Supply the glTF-Transform node_modules directory');
const {NodeIO,getBounds}=await import(pathToFileURL(path.join(deps,'@gltf-transform/core/dist/index.modern.js')));
const {ALL_EXTENSIONS}=await import(pathToFileURL(path.join(deps,'@gltf-transform/extensions/dist/index.modern.js')));
const {MeshoptDecoder,MeshoptEncoder}=await import(pathToFileURL(path.join(deps,'meshoptimizer/index.js')));
await Promise.all([MeshoptDecoder.ready,MeshoptEncoder.ready]);
const io=new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({'meshopt.decoder':MeshoptDecoder,'meshopt.encoder':MeshoptEncoder});
const source='/Users/sky/Projects/the-suborned/SourceArt/BoardingPavilionB';
const art=path.resolve('art');
const positions=[-3.15,-1.05,1.05,3.15],manifest=[];

function append(doc,level){
  const root=doc.getRoot(),scene=root.listScenes()[0],body=scene.listChildren();
  const bounds=getBounds(scene),size=bounds.max.map((v,i)=>v-bounds.min[i]),scale=12/Math.max(size[0],size[2]);
  const wrapper=doc.createNode('Approved Meshy body').setScale([scale,scale,scale]);
  if(size[2]>size[0])wrapper.setRotation([0,Math.SQRT1_2,0,Math.SQRT1_2]);
  for(const node of body){scene.removeChild(node);wrapper.addChild(node);}
  scene.addChild(wrapper);
  const fitted=getBounds(scene);
  wrapper.setTranslation([-(fitted.min[0]+fitted.max[0])/2,-fitted.min[1],-(fitted.min[2]+fitted.max[2])/2]);
  const surfaces=[];
  wrapper.traverse(node=>{
    const mesh=node.getMesh();if(!mesh)return;
    for(const primitive of mesh.listPrimitives()){
      const attribute=primitive.getAttribute('POSITION'),data=new Float32Array(attribute.getCount()*3);
      for(let i=0;i<attribute.getCount();i++)data.set(attribute.getElement(i,[]),i*3);
      const geometry=new THREE.BufferGeometry().setAttribute('position',new THREE.BufferAttribute(data,3));
      if(primitive.getIndices())geometry.setIndex(new THREE.BufferAttribute(primitive.getIndices().getArray(),1));
      geometry.applyMatrix4(new THREE.Matrix4().fromArray(node.getWorldMatrix()));
      surfaces.push(new THREE.Mesh(geometry,new THREE.MeshBasicMaterial({side:THREE.DoubleSide})));
    }
  });
  const ray=new THREE.Raycaster(),heights=positions.map(x=>{
    let lowest=Infinity;
    for(let dx=-.16;dx<=.161;dx+=.04)for(let dz=-.16;dz<=.161;dz+=.04){
      if(Math.hypot(dx,dz)>.17)continue;
      ray.set(new THREE.Vector3(x+dx,2,1.2+dz),new THREE.Vector3(0,1,0));
      const hit=ray.intersectObjects(surfaces,false)[0];if(hit)lowest=Math.min(lowest,hit.point.y);
    }
    if(!Number.isFinite(lowest))throw new Error('Cannot find pavilion soffit beneath fixture');
    return lowest-.026;
  });
  surfaces.forEach(mesh=>{mesh.geometry.dispose();mesh.material.dispose();});
  const original=root.listMaterials()[0],base=original.getBaseColorTexture(),normal=original.getNormalTexture();
  const brass=doc.createMaterial('Pavilion textured brass light bezels').setBaseColorFactor([.72,.48,.12,1])
    .setBaseColorTexture(base).setNormalTexture(normal).setMetallicFactor(.65).setRoughnessFactor(.4);
  const opal=doc.createMaterial('Pavilion textured opal downlight lenses').setBaseColorTexture(base).setNormalTexture(normal)
    .setMetallicFactor(0).setRoughnessFactor(.3).setEmissiveTexture(base).setEmissiveFactor([1,.76,.43]);
  for(const material of [brass,opal]){
    material.getBaseColorTextureInfo().copy(original.getBaseColorTextureInfo());
    material.getNormalTextureInfo().copy(original.getNormalTextureInfo());
  }
  opal.getEmissiveTextureInfo().copy(original.getBaseColorTextureInfo());
  const buffer=root.listBuffers()[0];
  function fitting(name,template,material,drop){
    const parts=positions.map((x,i)=>{const g=template.toNonIndexed();g.rotateX(Math.PI/2);g.translate(x,heights[i]-drop,1.2);
      const uv=g.attributes.uv;
      // This pearl atlas patch is the original underside; keep its fine grain.
      for(let i=0;i<uv.count;i++)uv.setXY(i,.43567+(uv.getX(i)-.5)*.012,.89358+(uv.getY(i)-.5)*.012);
      return g;
    });
    const primitive=doc.createPrimitive().setMaterial(material);
    for(const [key,semantic,type] of [['position','POSITION','VEC3'],['normal','NORMAL','VEC3'],['uv','TEXCOORD_0','VEC2']]){
      const array=new Float32Array(parts.reduce((n,g)=>n+g.attributes[key].array.length,0));let cursor=0;
      for(const g of parts){array.set(g.attributes[key].array,cursor);cursor+=g.attributes[key].array.length;}
      primitive.setAttribute(semantic,doc.createAccessor().setType(type).setArray(array).setBuffer(buffer));
    }
    const count=primitive.getAttribute('POSITION').getCount();
    primitive.setIndices(doc.createAccessor().setType('SCALAR').setArray(Uint16Array.from({length:count},(_,i)=>i)).setBuffer(buffer));
    scene.addChild(doc.createNode(name).setMesh(doc.createMesh(name).addPrimitive(primitive)));
    const triangles=count/3;template.dispose();parts.forEach(p=>p.dispose());return triangles;
  }
  const segments=[16,12,8][level];
  const added=fitting('Pavilion_RecessedBezels',new THREE.TorusGeometry(.14,.025,4,segments),brass,0)
    +fitting('Pavilion_OpalLenses',new THREE.CircleGeometry(.116,segments),opal,.008);
  return {level,addedTriangles:added,fixtures:4,fixturePositions:positions.map((x,i)=>[x,heights[i],1.2]),textureImages:root.listTextures().length,bounds:getBounds(scene)};
}

for(let level=0;level<3;level++){
  const name=`boarding-pavilion-b-LOD${level}`;
  const native=await io.read(`${source}/preserved/${name}.glb`),metrics=append(native,level);
  await fs.mkdir(`${source}/illuminated`,{recursive:true});
  await io.write(`${source}/illuminated/${name}.glb`,native);
  const web=await io.read(`${art}/boarding-pavilion-preserved/web/${name}.gltf`);append(web,level);
  await fs.mkdir(`${art}/boarding-pavilion-illuminated/web`,{recursive:true});
  await io.write(`${art}/boarding-pavilion-illuminated/web/${name}.gltf`,web);
  if(level===0)await io.write(`${art}/boarding-pavilion-illuminated/web/${name}.glb`,web);
  manifest.push(metrics);console.log(JSON.stringify(metrics));
}
const report={originalUntouched:`${source}/v1/boarding-pavilion-b.glb`,bodyAndArtwork:'Unchanged; appended separate recessed fittings',
  normalizedLengthMetres:12,fixturePositions:manifest[0].fixturePositions,newTextureImages:0,lods:manifest};
await fs.writeFile(`${source}/illuminated/fixtures.json`,JSON.stringify(report,null,2));
await fs.writeFile(`${art}/boarding-pavilion-illuminated/fixtures.json`,JSON.stringify(report,null,2));
