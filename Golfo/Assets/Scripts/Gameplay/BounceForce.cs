using Unity.VisualScripting;
using UnityEngine;

namespace lv.gameplay
{
    public class BounceForce : MonoBehaviour
    {
        [SerializeField] private float m_bouncePower;
        [SerializeField][Range(0, 1)] private float m_restitution;
        private Vector3 testVar;
        private Vector3 collPos;
        private Vector3 normPos;
        private Vector3 lastVel;


        private Rigidbody m_rigidbody;

        private void Awake()
        {
            m_rigidbody = GetComponent<Rigidbody>();
            testVar = new Vector3(0, 0, 0);
            collPos = new Vector3(0, 0, 0);
            normPos = new Vector3(0, 0, 0);
            lastVel = new Vector3(0, 0, 0);
        }

        private void Update()
        {
            // Debug Vectors
            //Debug.DrawLine(m_rigidbody.position, m_rigidbody.velocity * 100, Color.red);
            //Debug.DrawLine(collPos, testVar * 100, Color.green);
            //Debug.DrawLine(collPos, normPos * 100, Color.magenta);

            lastVel = m_rigidbody.velocity;
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Wall")
            {
                Vector3 wallNormal = collision.GetContact(0).normal.normalized;
                BallBounce(wallNormal);

                // Debug Vectors
                normPos = wallNormal;
            }
        }

        private void BallBounce(Vector3 wallNormal)
        {
            //vref = vi -2 * (vi * normal) * normal
            Vector3 dir = Vector3.Reflect(new Vector3(lastVel.x, 0, lastVel.z), wallNormal);

            // Debug Vectors
            testVar = dir;
            collPos = m_rigidbody.position;

            m_rigidbody.velocity = dir;
        }
    }
}
