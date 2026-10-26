using System; using Suborned.Sim; using System.Globalization;
static class P { static int Main(string[] a){ var i=Engine.Load(a[0]); var g=Engine.New(i); var car=g.S.Stand("d:Getaway bay");
  for(int k=1;k<a.Length;k++){ var s=a[k].Split(','); var p=new Pt(int.Parse(s[0]),int.Parse(s[1]));
    var f=g.S.Grid.NearestFree(p); Console.WriteLine($"{p} free {g.S.Grid.Free(p)} nearest {(f.HasValue? f.Value.ToString():"-")} car {(f.HasValue? G.Dist(f.Value,car)/100.0:0):0.0} m line {(f.HasValue? g.S.Sight(f.Value,car,g.R.Eye):false)}"); }
  return 0; } }
