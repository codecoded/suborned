using UnityEngine;

namespace Suborned.People
{
    /// <summary>One person of the people layer (brief 010 E): their record as the people export states it, written by the
    /// layer builder from out/people/, never by hand. Read-only at runtime; PeoplePicker shows it when the capsule is clicked.</summary>
    public sealed class PersonRecord : MonoBehaviour
    {
        public string id, personName, job, temperament, need, where;
        public string[] ties, routine;
        /// <summary>The routine line the snapshot clock falls in (-1: walking between two lines).</summary>
        public int now = -1;
        public bool indoors;
    }
}
