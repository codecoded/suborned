#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Suborned
{
    /// <summary>Review tools only, never the game: forwards Play Mode frame callbacks to a tool that lives in an Editor assembly
    /// (an Editor assembly cannot define a component). The tool creates it at runtime; it is never saved in a scene and is
    /// compiled out of every player build.</summary>
    public sealed class ReviewHost : MonoBehaviour
    {
        public Action Tick, Gui, Closed;

        void Update() => Tick?.Invoke();
        void OnGUI() => Gui?.Invoke();
        void OnDestroy() => Closed?.Invoke();
    }
}
#endif
