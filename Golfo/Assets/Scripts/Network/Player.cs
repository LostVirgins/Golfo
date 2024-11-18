using UnityEngine;

namespace lv.network
{
    public class Player : MonoBehaviour
    {
        public GameObject m_golfBall;
        public string m_sessionToken { get; private set; }

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
