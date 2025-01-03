using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace lv.network
{
    public class Player
    {
        public GameObject m_golfBall;
        public string m_sessionToken { get; private set; }
        public string m_username { get; private set; }

        public Vector3 m_netInitPos;
        public Vector3 m_netEndPos;
        public Vector3 m_netInitVel;
        public Vector3 m_netEndVel;

        public bool m_inHole = false;
        public List<int> m_score = new List<int> { 0, 0, 0, 0, 0, 0 };

        public Player(string username, string sessionToken)
        {
            m_username = username;
            m_sessionToken = sessionToken;
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            m_golfBall.transform.position = newPosition;
        }

        public int GetTotalScore()
        {
            return m_score.Sum();
        }
    }
}
