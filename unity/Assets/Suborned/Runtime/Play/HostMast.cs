using System;
using UnityEngine;

namespace Suborned.Play
{
    /// <summary>Brief 101 (Sky's pick B, the square's lamp mast; Vela's notes briefs/101/vela/lamp-mast.md): the world's camera masts the install step found in the
    /// scene (PlayInstall.Masts), each with its placed piece and one lens a head, so the host cues the head that saw with no search at runtime. View only.</summary>
    public sealed partial class HandoverHost
    {
        [Serializable] public sealed class WorldMast
        {
            /// <summary>The engine's camera id (Site: the watcher street/records-square/mast-camera-z0 is mast-z0).</summary>
            public string id = "";
            /// <summary>The placed kit piece: its renderers give the marks their height.</summary>
            public Transform piece;
            /// <summary>One lens a head, in the engine's head order (Camera.Facings): the piece's LOD0 parts named _Lens&lt;k&gt;.</summary>
            public Renderer[] lens = Array.Empty<Renderer>();
        }
        /// <summary>The world's masts whose piece has a lens a head (PlayInstall.Masts); empty in a scene installed before brief 101, or a kit without the lamp mast.</summary>
        public WorldMast[] worldMasts = Array.Empty<WorldMast>();
    }
}
