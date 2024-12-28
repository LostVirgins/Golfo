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

        private float m_rateSpeed;
        private float m_timeToDestiny;
        private float m_timeToStart;

        private float m_easedTime;
        private bool m_changeDir;
        private bool isServer;

        private void Awake()
        {
            m_rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            m_startPos = transform.position;
            m_rateSpeed = 1f / Vector3.Distance(m_startPos, m_destinyPos) * m_speed;
            m_changeDir = false;

            //if (!isServer)
            //{
            //    m_networkedPos = transform.position;
            //}
        }

        private void Update()
        {
            Movement();

            //if (isServer)
            //    BroadcastPosition();
            //else
            //    InterpolatePosition();
        }

        private void Movement()
        {
            if (m_timeToDestiny <= 1f && !m_changeDir)
            {
                m_timeToDestiny += Time.deltaTime * m_rateSpeed;
                m_easedTime = Easing.InOutSine(m_timeToDestiny);

                transform.position = Vector3.Lerp(m_startPos, m_destinyPos, m_easedTime);
            }
            else
            {
                m_changeDir = true;
                m_timeToDestiny = 0f;
                //Debug.Log("changed_1");
            }

            if (m_timeToStart <= 1f && m_changeDir)
            {
                m_timeToStart += Time.deltaTime * m_rateSpeed;
                m_easedTime = Easing.InOutSine(m_timeToStart);

                transform.position = Vector3.Lerp(m_destinyPos, m_startPos, m_easedTime);
            }
            else
            {
                m_changeDir = false;
                m_timeToStart = 0f;
                //Debug.Log("changed_2");
            }
        }

        private void BroadcastPosition()
        {
            Packet posData = new Packet();
            posData.WriteByte((byte)PacketType.dynamicObstacle_position);
            posData.WriteFloat(m_easedTime);
            posData.WriteBool(m_changeDir);
            NetworkManager.Instance.EnqueueSend(new PacketData(posData, NetworkManager.Instance.m_hostEndPoint, true));
        }

        private void InterpolatePosition()
        {
            // Clen this later
            if (m_timeToDestiny <= 1f && !m_changeDir)
            {
                m_timeToDestiny += Time.deltaTime * m_rateSpeed;
                m_easedTime = Easing.InOutSine(m_timeToDestiny);

                transform.position = Vector3.Lerp(m_startPos, m_destinyPos, m_easedTime);
            }
            else
            {
                m_changeDir = true;
                m_timeToDestiny = 0f;
                //Debug.Log("changed_1");
            }

            if (m_timeToStart <= 1f && m_changeDir)
            {
                m_timeToStart += Time.deltaTime * m_rateSpeed;
                m_easedTime = Easing.InOutSine(m_timeToStart);

                transform.position = Vector3.Lerp(m_destinyPos, m_startPos, m_easedTime);
            }
            else
            {
                m_changeDir = false;
                m_timeToStart = 0f;
                //Debug.Log("changed_2");
            }

            transform.position = Vector3.Lerp(transform.position, m_networkedPos, Time.deltaTime * m_speed);
        }

        public void ReceiveNetworkPosition(Vector3 position)
        {
            m_networkedPos = position;
        }
    }
}
