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
            public GameObject spawnPoint;
            public int par;
        }

        public List<Hole> m_Holes = new List<Hole>();
    }
}
