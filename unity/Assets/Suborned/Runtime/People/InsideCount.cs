using UnityEngine;

namespace Suborned.People
{
    /// <summary>How many simulated people are inside at one place at the snapshot clock (brief 010 E; exteriors only: people
    /// inside are not drawn). Written by the layer builder at the place's door; PeoplePicker tags it `inside: N`.</summary>
    public sealed class InsideCount : MonoBehaviour
    {
        public string place;
        public int count;
    }
}
