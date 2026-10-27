using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Suborned.Scratch
{
    /// <summary>Brief 073 SCRATCH (Glint): the vintage-print finish's knobs, as a volume component. Print073 puts one global volume
    /// (priority 1000) holding this on top of the shipped volumes only while the toggle is on; the shipped grade assets are never edited.
    /// Default values are inert (strength 0).</summary>
    [Serializable, VolumeComponentMenu("Suborned/Scratch/Print 073")]
    public sealed class Print073Grade : VolumeComponent
    {
        [Tooltip("Master mix of the whole print pass (0 = off).")]
        public ClampedFloatParameter strength = new(0f, 0f, 1f);

        [Header("Specular cap (HDR, before post)")]
        [Tooltip("Scene-linear max channel where the soft shoulder starts.")] public MinFloatParameter capKnee = new(1.6f, 0f);
        [Tooltip("How far above the knee a value can still climb (asymptote = knee + range).")] public MinFloatParameter capRange = new(0.8f, 0.01f);
        [Tooltip("1 = no cap.")] public ClampedFloatParameter capMix = new(0f, 0f, 1f);

        [Header("Faded warm grade (after post, display space)")]
        [Tooltip("The darkest the print can go: the ink, lifted (teal-black).")] public ColorParameter ink = new(new Color(0.09f, 0.12f, 0.14f));
        [Tooltip("The brightest the print can go: the paper.")] public ColorParameter paper = new(new Color(0.98f, 0.95f, 0.87f));
        [Tooltip("0 = the frame's own black and white, 1 = fully mapped onto ink..paper.")] public ClampedFloatParameter fade = new(0f, 0f, 1f);
        public ClampedFloatParameter saturation = new(1f, 0f, 1.5f);
        [Tooltip("Extra desaturation in the highlights.")] public ClampedFloatParameter highlightDesat = new(0f, 0f, 1f);
        [Tooltip("Warm cast: + toward amber, multiplies R up and B down.")] public ClampedFloatParameter warmth = new(0f, -0.2f, 0.2f);

        [Header("Ink lines (depth crease/silhouette; needs the depth texture)")]
        public ClampedFloatParameter inkLines = new(0f, 0f, 1f);
        [Tooltip("Depth second-difference (m) where a line starts / is full.")] public MinFloatParameter inkLow = new(0.05f, 0f);
        public MinFloatParameter inkHigh = new(0.25f, 0.001f);

        [Header("Halftone (screen-fixed, px at 1080p)")]
        public ClampedFloatParameter halftone = new(0f, 0f, 1f);
        public ClampedFloatParameter halftoneCell = new(4f, 2f, 16f);
        public ClampedFloatParameter halftoneAngle = new(15f, 0f, 90f);
        [Tooltip("0 = one ink screen on luminance, 1 = CMY screens at 15/75/0 degrees.")] public ClampedFloatParameter halftoneColour = new(0f, 0f, 1f);

        [Header("Paper")]
        [Tooltip("Fine grain (1-4 px at 1080p), multiplicative.")] public ClampedFloatParameter grain = new(0f, 0f, 0.2f);
        [Tooltip("Low-frequency mottling (45-140 px), multiplicative.")] public ClampedFloatParameter mottle = new(0f, 0f, 0.2f);
        public ClampedFloatParameter vignette = new(0f, 0f, 0.5f);

        public bool IsActive() => strength.value > 0f;
    }
}
