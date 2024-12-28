using lv.network;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

namespace lv.gameplay
{
    public class DynamicObstacles : MonoBehaviour
    {
        [SerializeField] private Vector3 m_destinyPos;
        [SerializeField] private float m_speed;

        private Rigidbody m_rb;

        private Vector3 m_startPos;
        private Vector3 m_networkedPos;

        public float m_rateSpeed;
        public float m_totalTime;
        public float m_easedTime;
        public bool m_reverse;

        private bool isServer;

        private void Awake()
        {
            m_rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            m_startPos = transform.position;
            m_rateSpeed = 1f / Vector3.Distance(m_startPos, m_destinyPos) * m_speed;
            m_totalTime = 0f;
            m_easedTime = 0f;
            m_reverse = false;

            //if (!isServer)
            //{
            //    m_networkedPos = transform.position;
            //}
        }

        private void Update()
        {
            Movement();
        }

        private void Movement()
        {
            if (m_totalTime > 1f) m_reverse = true;
            else if (m_totalTime < 0f) m_reverse = false;

            if (m_reverse)
                m_totalTime -= Time.deltaTime * m_rateSpeed;
            else
                m_totalTime += Time.deltaTime * m_rateSpeed;

            m_easedTime = Easing.InOutSine(m_totalTime);
            transform.position = Vector3.Lerp(m_startPos, m_destinyPos, m_easedTime);
        }
    }
}
