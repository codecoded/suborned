using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Suborned.World.Review
{
    /// <summary>Renders the flyby (flyby.json over the built scene) to &lt;repo&gt;/out/shots/flyby/: flyby.mp4 (H.264, yuv420p, at the
    /// path's size and fps; every frame piped raw into ffmpeg (render.ffmpeg) as it renders, so the video is never re-encoded from the
    /// JPEGs), frame-NNNNN.jpg, one contact image per stop (mid-hold), contact-sheet.png, and index.html (a frame player, the video
    /// and the contact sheet).
    /// Menu: Suborned/World/Flyby (window: watch live in the Game view) and Suborned/World/Render Flyby Frames. Batchmode
    /// (needs a GPU: no -nographics):
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Suborned.World.Review.Flyby.Render [-world &lt;export&gt;] [-flyby &lt;json&gt;]</summary>
    public static class Flyby
    {
        public static string OutDir => Path.Combine(HarbourWorldBuilder.RepoRoot, "out", "shots", "flyby");

        public static FlybyPath Load() => new(FlybyPath.DefaultPath, new WorldIndex(WorldReader.Load(RenderSession.ExportPath)));

        /// <summary>Batchmode entry: renders every frame synchronously.</summary>
        public static void Render() => HarbourWorldBuilder.Batch(() =>
        {
            if (!RenderSession.EnsureScene()) return;
            var job = new Job(Load());
            while (!job.Step(int.MaxValue)) { }
        });

        [MenuItem("Suborned/World/Render Flyby Frames")]
        public static void RenderMenu() => HarbourWorldBuilder.Batch(() =>
        {
            if (!RenderSession.EnsureScene()) return;
            var job = new Job(Load());
            void Tick()
            {
                bool done;
                try
                {
                    done = job.Step(8);
                    if (!done && EditorUtility.DisplayCancelableProgressBar("Flyby", $"frame {job.Frame}/{job.Frames}", (float)job.Frame / job.Frames)) { job.Cancel(); done = true; }
                }
                catch (Exception e) { job.Cancel(); Debug.LogException(e); done = true; }
                if (done) { EditorApplication.update -= Tick; EditorUtility.ClearProgressBar(); }
            }
            EditorApplication.update += Tick;
        });

        /// <summary>One render pass, steppable (the Editor stays responsive between steps).</summary>
        public sealed class Job
        {
            readonly FlybyPath path;
            readonly RenderSession session;
            readonly Camera cam;
            readonly List<(string name, string file)> contacts = new();
            public readonly int Frames;
            public int Frame { get; private set; }
            int contact;
            bool finished;
            System.Diagnostics.Process ffmpeg;
            Stream video;
            readonly StringBuilder ffmpegLog = new();
            public string Mp4 => Path.Combine(OutDir, "flyby.mp4");

            public Job(FlybyPath path)
            {
                this.path = path;
                Frames = Mathf.FloorToInt(path.Duration * path.Fps) + 1;
                if (Directory.Exists(OutDir)) foreach (var f in Directory.GetFiles(OutDir)) File.Delete(f);
                Directory.CreateDirectory(OutDir);
                session = new RenderSession();
                cam = RenderSession.TempCamera("flyby-render");
                path.Evaluate(0f).Apply(cam);
                session.Grab(cam, 64, 64); // the first render after opening a scene comes out flat
                if (!string.IsNullOrEmpty(path.Ffmpeg)) StartVideo();
            }

            void StartVideo()
            {
                var exe = File.Exists(path.Ffmpeg) ? path.Ffmpeg : "ffmpeg";
                var psi = new System.Diagnostics.ProcessStartInfo(exe,
                    $"-hide_banner -loglevel error -y -f rawvideo -pix_fmt rgb24 -s {path.Width}x{path.Height} -r {path.Fps} -i - " +
                    $"-vf vflip -c:v libx264 -preset {path.Preset} -crf {path.Crf} -pix_fmt yuv420p -r {path.Fps} -movflags +faststart \"{Mp4}\"")
                { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
                try { ffmpeg = System.Diagnostics.Process.Start(psi); }
                catch (Exception e) { throw new InvalidOperationException($"World: flyby: cannot start ffmpeg `{exe}` ({e.Message}); set render.ffmpeg in flyby.json", e); }
                ffmpeg.ErrorDataReceived += (_, a) => { if (a.Data != null) lock (ffmpegLog) ffmpegLog.AppendLine(a.Data); };
                ffmpeg.BeginErrorReadLine();
                video = ffmpeg.StandardInput.BaseStream;
            }

            /// <summary>Closes ffmpeg's input and waits for the file; throws if it failed.</summary>
            void FinishVideo()
            {
                if (ffmpeg == null) return;
                video.Flush(); video.Close();
                if (!ffmpeg.WaitForExit(600000)) { ffmpeg.Kill(); throw new IOException("World: flyby: ffmpeg did not finish in 10 min"); }
                int code = ffmpeg.ExitCode; ffmpeg.Dispose(); ffmpeg = null;
                if (code != 0 || !File.Exists(Mp4)) throw new IOException($"World: flyby: ffmpeg exited {code}: {ffmpegLog}");
                Debug.Log($"World: flyby video {Mp4} ({new FileInfo(Mp4).Length / 1048576.0:F1} MiB, {path.Width}x{path.Height} @ {path.Fps} fps, {Frames / (float)path.Fps:F1}s)");
            }

            /// <summary>Renders up to n frames (then the contact images); true when finished.</summary>
            public bool Step(int n)
            {
                if (finished) return true;
                for (int i = 0; i < n && Frame < Frames; i++, Frame++)
                {
                    var pose = path.Evaluate(Frame / (float)path.Fps);
                    pose.Apply(cam); session.ShadowDistance = pose.ShadowDistance;
                    var tex = session.Grab(cam, path.Width, path.Height);
                    File.WriteAllBytes(Path.Combine(OutDir, $"frame-{Frame:D5}.jpg"), tex.EncodeToJPG(path.Quality));
                    if (video != null) { var raw = tex.GetRawTextureData<byte>(); video.Write(raw.ToArray(), 0, raw.Length); }
                }
                if (Frame < Frames) return false;
                FinishVideo();
                for (; contact < path.Keys.Count; contact++)
                {
                    var k = path.Keys[contact];
                    if (!k.Contact) continue;
                    var pose = path.Evaluate(k.Start + k.Hold * 0.5f);
                    pose.Apply(cam); session.ShadowDistance = pose.ShadowDistance;
                    var file = $"stop-{contact:D2}.png";
                    File.WriteAllBytes(Path.Combine(OutDir, file), session.Grab(cam, path.ContactWidth, path.ContactHeight).EncodeToPNG());
                    contacts.Add((k.Name, file));
                }
                Sheet();
                Html();
                Debug.Log($"World: flyby {Frames} frames ({path.Width}x{path.Height} @ {path.Fps} fps, {path.Duration:F1}s, {contacts.Count} stops) → {OutDir}");
                Cancel();
                return true;
            }

            public void Cancel()
            {
                finished = true;
                if (ffmpeg != null) { try { video.Close(); ffmpeg.Kill(); } catch (Exception) { } ffmpeg.Dispose(); ffmpeg = null; }
                session.Dispose();
                if (cam) UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }

            /// <summary>contact-sheet.png: the stop images in a 4-wide grid.</summary>
            void Sheet()
            {
                int cols = 4, rows = (contacts.Count + cols - 1) / cols, w = path.ContactWidth, h = path.ContactHeight, pad = 6;
                var sheet = new Texture2D(cols * (w + pad) + pad, rows * (h + pad) + pad, TextureFormat.RGB24, false);
                var fill = Enumerable.Repeat(new Color32(24, 28, 34, 255), sheet.width * sheet.height).ToArray();
                sheet.SetPixels32(fill);
                for (int i = 0; i < contacts.Count; i++)
                {
                    var t = new Texture2D(2, 2);
                    t.LoadImage(File.ReadAllBytes(Path.Combine(OutDir, contacts[i].file)));
                    int c = i % cols, r = rows - 1 - i / cols;
                    sheet.SetPixels(pad + c * (w + pad), pad + r * (h + pad), w, h, t.GetPixels());
                    UnityEngine.Object.DestroyImmediate(t);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(OutDir, "contact-sheet.png"), sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }

            void Html()
            {
                var keys = path.Keys.Where(k => k.Contact).ToList();
                var starts = string.Join(",", keys.Select(k => Mathf.RoundToInt(k.Start * path.Fps)));
                var names = string.Join(",", keys.Select(k => "\"" + k.Name.Replace("\"", "'") + "\""));
                var sb = new StringBuilder();
                sb.Append("<!doctype html><meta charset=utf-8><title>Harbour District flyby</title><style>body{background:#15181d;color:#dde;font:14px system-ui;margin:16px}img#f{width:100%;max-width:")
                  .Append(path.Width).Append("px;display:block;background:#000}button{font:inherit;margin:2px;padding:4px 10px}#bar{margin:8px 0}input[type=range]{width:")
                  .Append(Mathf.Min(path.Width, 900)).Append("px}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;max-width:1400px}.grid figure{margin:0}.grid img{width:100%;cursor:pointer}figcaption{font-size:12px;color:#aab}</style>")
                  .Append($"<h2>Harbour District flyby</h2><p>{Frames} frames, {path.Width}x{path.Height} at {path.Fps} fps ({path.Duration:F1} s), {keys.Count} stops. Built from <code>{Path.GetFileName(RenderSession.ExportPath)}</code> and <code>flyby.json</code>, {DateTime.Now:yyyy-MM-dd HH:mm}. Space = play/pause, arrows = step, [ ] = speed.</p>")
                  .Append("<img id=f src='frame-00000.jpg'><div id=bar><button id=pp>pause</button><button data-s=0.25>0.25x</button><button data-s=0.5>0.5x</button><button data-s=1>1x</button><button data-s=2>2x</button><button data-s=4>4x</button> <span id=lab></span><br><input id=sc type=range min=0 max=")
                  .Append(Frames - 1).Append(" value=0></div><h3>Video</h3><p><a href='flyby.mp4'>flyby.mp4</a></p><video src='flyby.mp4' controls preload=metadata style='width:100%;max-width:960px'></video><h3>Stops</h3><p><a href='contact-sheet.png'>contact-sheet.png</a></p><div class=grid>");
                for (int i = 0; i < contacts.Count; i++)
                    sb.Append($"<figure><img src='{contacts[i].file}' data-k={i}><figcaption>{i + 1}. {System.Net.WebUtility.HtmlEncode(contacts[i].name)}</figcaption></figure>");
                sb.Append("</div><script>const N=").Append(Frames).Append(",FPS=").Append(path.Fps).Append(",S=[").Append(starts).Append("],L=[").Append(names).Append("];")
                  .Append(@"let i=0,play=true,speed=1,acc=0,last=performance.now();const f=document.getElementById('f'),sc=document.getElementById('sc'),lab=document.getElementById('lab'),pp=document.getElementById('pp');
const cache=[];function name(n){return 'frame-'+String(n).padStart(5,'0')+'.jpg'}
function show(n){i=Math.max(0,Math.min(N-1,n));f.src=name(i);sc.value=i;let k=0;while(k+1<S.length&&S[k+1]<=i)k++;lab.textContent=`frame ${i+1}/${N}  ${(i/FPS).toFixed(1)}s  ${speed}x  stop ${k+1}: ${L[k]}`;for(let j=1;j<6;j++){const m=i+j;if(m<N&&!cache[m]){cache[m]=new Image();cache[m].src=name(m)}}}
function tick(t){const dt=(t-last)/1000;last=t;if(play){acc+=dt*FPS*speed;const s=Math.floor(acc);if(s>0){acc-=s;show(i+s>=N?0:i+s)}}requestAnimationFrame(tick)}
pp.onclick=()=>{play=!play;pp.textContent=play?'pause':'play'};document.querySelectorAll('[data-s]').forEach(b=>b.onclick=()=>{speed=+b.dataset.s;show(i)});
sc.oninput=()=>show(+sc.value);document.querySelectorAll('[data-k]').forEach(b=>b.onclick=()=>{show(S[+b.dataset.k]);scrollTo(0,0)});
onkeydown=e=>{if(e.key===' '){pp.click();e.preventDefault()}if(e.key==='ArrowRight')show(i+1);if(e.key==='ArrowLeft')show(i-1);if(e.key===']'){speed=Math.min(8,speed*2);show(i)}if(e.key==='['){speed=Math.max(0.125,speed/2);show(i)}};
show(0);requestAnimationFrame(tick);</script>");
                File.WriteAllText(Path.Combine(OutDir, "index.html"), sb.ToString());
            }
        }
    }
}
