using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace lv.gameplay
{
    public class MapData : MonoBehaviour
    {
        [Serializable]
        public struct Hole
        {
            public int par;
            public GameObject spawnPoint;
            public GameObject bound;
        }

        public List<Hole> m_Holes = new List<Hole>();
    }
}
