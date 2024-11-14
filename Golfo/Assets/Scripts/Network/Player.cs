using UnityEngine;

namespace lv.network
{
    public class Player
    {
        public string m_sessionToken { get; private set; }
        public Vector3 m_position { get; set; }

        public Player()
        {
            m_sessionToken = "";
            m_position = Vector3.zero;
        }

        public Player(string sessionToken)
        {
            m_sessionToken = sessionToken;
            m_position = Vector3.zero;
        }

        public Player(string sessionToken, Vector3 position)
        {
            m_sessionToken = sessionToken;
            m_position = position;
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            m_position = newPosition;
        }
    }
}
