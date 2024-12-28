using UnityEngine;

namespace lv.network
{
    public class Player : MonoBehaviour
    {
        public GameObject m_golfBall;
        public string m_sessionToken { get; private set; }

        public Vector3 m_initialPos;
        public Vector3 m_networkedPos;

        public Player(string sessionToken)
        {
            m_sessionToken = sessionToken;
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            m_golfBall.transform.position = newPosition;
        }
    }
}
