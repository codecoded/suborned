using System.Collections.Generic;
using UnityEngine;
using Game = Suborned.Sim.Game;

namespace Suborned.Play
{
    public sealed partial class HandoverHost
    {
        /// <summary>Brief 078: the car damage rig (Assets/Suborned/Generated/Fx/CarFx.prefab, PlayInstall); none = made in code at run time.</summary>
        public GameObject carFx;
        CarFxPool carFxPool; Game fxGame; float fxClock = -1f;
        readonly List<CarFxState> fxCars = new();

        /// <summary>The mission time the car effects draw (s): the views' eased time between ticks, never going back while the run goes on;
        /// held while paused, in a scene, frozen or over. -1 before the first frame.</summary>
        public float CarFxClock => fxClock;
        /// <summary>The pool (for checks and captures); null until the first frame with a game.</summary>
        public CarFxPool CarFxPool => carFxPool;
        /// <summary>Whether a car's damage is drawn in the world this frame (the HUD then leaves out its flat smoke, flame and flash).</summary>
        public bool CarFxDrawn(string id) => carFxPool != null && carFxPool.Drawn(id);

        /// <summary>Brief 078: each frame after the views moved: the damaged cars from the engine (read only), the clock, the rigs.</summary>
        void CarFxFrame()
        {
            var g = Game; if (g == null || views == null) return;
            if (carFxPool == null) carFxPool = new CarFxPool(transform, carFx);
            float alpha = Paused || g.Over ? 1f : Mathf.Clamp01(acc / 0.1f);
            float t = (g.Tick - 1 + alpha) / Game.Hz; // the views draw from the last tick to this one
            if (CarFxPool.TimeOverride >= 0f) t = CarFxPool.TimeOverride;
            if (fxGame != g || t < fxClock - 0.5f) { fxGame = g; fxClock = t; carFxPool.ReleaseAll(); } // a new run (Begin, a restart, a load)
            fxClock = Mathf.Max(fxClock, t); float now = fxClock;
            fxCars.Clear(); float blastM = g.R.CarBlastCm / 100f;
            foreach (var id in CarIdsNow())
            {
                Game.CarTarget c = null; try { c = g.CarOf(id); } catch { }
                var key = "car/" + id.Substring(4);
                if (c == null || c.DisabledAt < 0 && c.BurningAt < 0 && !c.Blown) { views.CarTint(key, 1f); continue; }
                var vt = views.Find(key); bool shown = vt && vt.gameObject.activeInHierarchy;
                Vector3 at = shown ? vt.position : frame.World(g.CarAt(id) ?? c.At);
                var model = shown ? vt.Find("model") : null; var fwd = model ? model.forward : shown ? vt.forward : Vector3.forward; fwd.y = 0f;
                var st = new CarFxState
                {
                    Id = id, At = at, Facing = fwd.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(fwd.normalized, Vector3.up) : Quaternion.identity,
                    DisabledS = c.DisabledAt >= 0 ? c.DisabledAt / (float)Game.Hz : -1f, BurningS = c.BurningAt >= 0 ? c.BurningAt / (float)Game.Hz : -1f,
                    BlastS = c.BlastAt >= 0 ? c.BlastAt / (float)Game.Hz : -1f, Blown = c.Blown, BlastM = blastM,
                };
                fxCars.Add(st); views.CarTint(key, CarFxPool.Tint(st, now));
            }
            carFxPool.Frame(now, fxCars, focus);
        }
    }
}
