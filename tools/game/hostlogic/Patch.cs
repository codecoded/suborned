// A test-only copy of Unity's managed assemblies with every native call replaced: void and default returns, and a few that read fields on UnityEngine.HostLogicShim (the clock, the
// screen, the data paths). The player's own compiled scripts then load in plain dotnet and the host's decisions run with no Editor, player or graphics device (tools/game/hostlogic.ts).
// The copy lives under out/ and is never built into anything: a native call answering a default means these checks test logic, never what Unity itself does.
using System; using System.IO; using System.Linq; using Mono.Cecil; using Mono.Cecil.Cil;
static class P {
  // type.getter -> field on UnityEngine.HostLogicShim (added to CoreModule)
  static readonly (string type,string method,string field)[] Ours = {
    ("UnityEngine.Time","get_unscaledTime","UnscaledTime"),("UnityEngine.Time","get_time","GameTime"),("UnityEngine.Time","get_realtimeSinceStartup","UnscaledTime"),("UnityEngine.Time","get_unscaledDeltaTime","Delta"),("UnityEngine.Time","get_deltaTime","Delta"),("UnityEngine.Time","get_frameCount","Frame"),
    ("UnityEngine.Screen","get_width","ScreenW"),("UnityEngine.Screen","get_height","ScreenH"),
    ("UnityEngine.Application","get_streamingAssetsPath","Streaming"),("UnityEngine.Application","get_persistentDataPath","Persistent"),("UnityEngine.Application","get_dataPath","Data"),
  };
  static readonly string[] Handles = { "UnityEngine.GameObject.get_transform", "UnityEngine.Component.get_transform", "UnityEngine.Component.get_gameObject", "UnityEngine.Transform.get_parent", "UnityEngine.Transform.get_root" };
  static void Default(MethodDefinition m, ModuleDefinition mod){
    m.IsInternalCall=false; m.IsPInvokeImpl=false; m.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;
    var b=new MethodBody(m); m.Body=b; var il=b.GetILProcessor();
    foreach(var p in m.Parameters) if(p.ParameterType.IsByReference && p.IsOut){ var el=((ByReferenceType)p.ParameterType).ElementType; il.Emit(OpCodes.Ldarg,p); if(el.IsValueType||el.IsGenericParameter) il.Emit(OpCodes.Initobj,el); else {il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Stind_Ref);} }
    var r=m.ReturnType;
    if(r.MetadataType==MetadataType.Void){ il.Emit(OpCodes.Ret); return; }
    if(r.IsValueType||r.IsGenericParameter){ var v=new VariableDefinition(r); b.Variables.Add(v); b.InitLocals=true; il.Emit(OpCodes.Ldloc,v); il.Emit(OpCodes.Ret); return; }
    il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Ret);
  }
  /// <summary>The player's compiled scripts, with what draws or sounds made silent: the host's decisions stay as compiled. Add a member here only when it draws, plays or touches a GameObject.</summary>
  static readonly string[] Silent = { "Suborned.Play.HostViews" };
  static readonly string[] SilentHost = { "AudioInit", "AudioLog", "AudioTick", "AudioFrame", "Save" };
  static int Player(string dll, string outDir){
    Directory.CreateDirectory(outDir); var res=new DefaultAssemblyResolver(); res.AddSearchDirectory(Path.GetDirectoryName(dll));
    var asm=AssemblyDefinition.ReadAssembly(dll,new ReaderParameters{AssemblyResolver=res,ReadSymbols=false}); var mod=asm.MainModule; int n=0;
    foreach(var t in mod.GetTypes()){
      bool all=Silent.Contains(t.FullName), host=t.FullName=="Suborned.Play.HandoverHost";
      if(!all&&!host) continue;
      foreach(var m in t.Methods){ if(!m.HasBody||m.IsStatic&&m.IsConstructor) continue; if(all||(host&&SilentHost.Contains(m.Name))){ Default(m,mod); n++; } }
    }
    asm.Write(Path.Combine(outDir,"check.dll")); return 0;
  }
  static int Main(string[] a){
    if(a[0]=="player") return Player(a[1],a[2]);
    string from=a[0], to=a[1]; Directory.CreateDirectory(to); var res=new DefaultAssemblyResolver(); res.AddSearchDirectory(from); foreach(var x in a.Skip(2)) res.AddSearchDirectory(Path.GetDirectoryName(x));
    int n=0,externs=0;
    foreach(var f in Directory.GetFiles(from,"UnityEngine*.dll").Concat(a.Skip(2))){
      var asm=AssemblyDefinition.ReadAssembly(f,new ReaderParameters{AssemblyResolver=res}); var mod=asm.MainModule; bool core=Path.GetFileName(f)=="UnityEngine.CoreModule.dll";
      TypeDefinition shim=null;
      if(core){ shim=new TypeDefinition("UnityEngine","HostLogicShim",TypeAttributes.Public|TypeAttributes.Abstract|TypeAttributes.Sealed|TypeAttributes.Class,mod.TypeSystem.Object); mod.Types.Add(shim);
        foreach(var (nm,t) in new[]{("UnscaledTime",mod.TypeSystem.Single),("GameTime",mod.TypeSystem.Single),("Delta",mod.TypeSystem.Single),("Frame",mod.TypeSystem.Int32),("ScreenW",mod.TypeSystem.Int32),("ScreenH",mod.TypeSystem.Int32),("Streaming",mod.TypeSystem.String),("Persistent",mod.TypeSystem.String),("Data",mod.TypeSystem.String)})
          shim.Fields.Add(new FieldDefinition(nm,FieldAttributes.Public|FieldAttributes.Static,t)); }
      foreach(var t in mod.GetTypes().ToList()) foreach(var m in t.Methods){
        if(core&&t.FullName=="UnityEngine.PlayerPrefs"&&m.Name.StartsWith("Get")&&m.Parameters.Count==2){ m.IsInternalCall=false; m.ImplAttributes=MethodImplAttributes.IL|MethodImplAttributes.Managed; var b0=new MethodBody(m); m.Body=b0; var i0=b0.GetILProcessor(); i0.Emit(OpCodes.Ldarg_1); i0.Emit(OpCodes.Ret); n++; continue; } // nothing saved: the default given
        if(core&&Handles.Contains(t.FullName+"."+m.Name)){ // a handle with no native object behind it: Unity's own `if (x)` still reads false, and calls on it answer defaults
          m.IsInternalCall=false; m.ImplAttributes=MethodImplAttributes.IL|MethodImplAttributes.Managed; var b=new MethodBody(m); m.Body=b; var il=b.GetILProcessor();
          il.Emit(OpCodes.Ldtoken,m.ReturnType); il.Emit(OpCodes.Call,mod.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"))); il.Emit(OpCodes.Call,mod.ImportReference(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("GetUninitializedObject"))); il.Emit(OpCodes.Castclass,m.ReturnType); il.Emit(OpCodes.Ret); n++; continue; }
        if(core){ var o=Ours.FirstOrDefault(x=>x.type==t.FullName&&x.method==m.Name); if(o.field!=null){ m.IsInternalCall=false; m.ImplAttributes=MethodImplAttributes.IL|MethodImplAttributes.Managed; var b=new MethodBody(m); m.Body=b; var il=b.GetILProcessor(); il.Emit(OpCodes.Ldsfld,shim.Fields.First(x=>x.Name==o.field)); il.Emit(OpCodes.Ret); n++; continue; } }
        if(m.IsInternalCall||m.IsPInvokeImpl||t.FullName=="UnityEngine.Bindings.ThrowHelper"&&m.HasBody&&!m.IsConstructor){ Default(m,mod); externs++; } // ThrowHelper: "no native object" is every object here
      }
      asm.Write(Path.Combine(to,Path.GetFileName(f)));
    }
    Console.WriteLine($"patched {externs} native methods to defaults, {n} to shim fields"); return 0;
  }
}
