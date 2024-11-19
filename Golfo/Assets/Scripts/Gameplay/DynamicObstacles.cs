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

        private float m_rateSpeed;
        private float m_timeToDestiny;
        private float m_timeToStart;

        private bool m_changeDir;

        private void Awake()
        {
            m_rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            m_startPos = transform.position;
            m_rateSpeed = 1f / Vector3.Distance(m_startPos, m_destinyPos) * m_speed;
            m_changeDir = false;

        }

        private void Update()
        {
            //Easing.InOutSine()

            if (m_timeToDestiny <= 1f && !m_changeDir)
            {
                m_timeToDestiny += Time.deltaTime * m_rateSpeed;
                float easedTime = Easing.InOutSine(m_timeToDestiny);

                transform.position = Vector3.Lerp(m_startPos, m_destinyPos, easedTime);
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
                float easedTime = Easing.InOutSine(m_timeToStart);

                transform.position = Vector3.Lerp(m_destinyPos, m_startPos, easedTime);
            }
            else
            {
                m_changeDir = false;
                m_timeToStart = 0f;
                //Debug.Log("changed_2");
            }
        }
    }
}
