using UnityEngine;

namespace lv.network
{
    public class Player : MonoBehaviour
    {
        public GameObject m_golfBall;
        public string m_sessionToken { get; private set; }
        public bool m_isHost;

        public Player(string sessionToken, bool isHost)
        {
            m_sessionToken = sessionToken;
            m_isHost = isHost;
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            m_golfBall.transform.position = newPosition;
        }
    }
}
