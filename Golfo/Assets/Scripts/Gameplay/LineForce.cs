using lv.network;
using UnityEngine;

namespace lv.gameplay
{
    public class LineForce : MonoBehaviour
    {
        [SerializeField] private float shotPower;
        [SerializeField] private float stopVelocity = 0.15f; //The velocity below which the rigidbody will be considered as stopped

        [SerializeField] private LineRenderer lineRenderer;

        private bool isIdle;
        private bool isAiming;

        private Rigidbody rigidbody;

        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();

            isAiming = false;
            lineRenderer.enabled = false;
        }

        private void Update()
        {
            if (rigidbody.velocity.magnitude < stopVelocity)
                Stop();

            ProcessAim();
        }

        private void OnMouseDown()
        {
            if (isIdle)
                isAiming = true;
        }

        private void ProcessAim()
        {
            if (!isAiming || !isIdle)
                return;

            Vector3? worldPoint = CastMouseClickRay();

            if (!worldPoint.HasValue)
                return;

            DrawLine(worldPoint.Value);

            if (Input.GetMouseButtonUp(0))
                Shoot(worldPoint.Value);
        }

        private void Shoot(Vector3 worldPoint)
        {
            isAiming = false;
            lineRenderer.enabled = false;

            Vector3 horizontalWorldPoint = new Vector3(worldPoint.x, transform.position.y, worldPoint.z);

            Vector3 direction = (horizontalWorldPoint - transform.position).normalized;
            float strength = Vector3.Distance(transform.position, horizontalWorldPoint);

            rigidbody.AddForce(-direction * strength * shotPower);
            isIdle = false;

            // Notify Server
            Packet packet = new Packet();
            packet.WriteByte((byte)PacketType.ball_strike);
            packet.WriteString("hekbas_todo_use_token_:)");
            packet.WriteVector3(direction);
            packet.WriteFloat(strength);
            packet.WriteFloat(shotPower);

            NetworkManager.Instance.m_sendQueue.Enqueue(new PacketData(packet, NetworkManager.Instance.m_hostEndPoint));
        }

        private void DrawLine(Vector3 worldPoint)
        {
            Vector3[] positions = {
            new Vector3(transform.position.x, transform.position.y, transform.position.z),
            new Vector3(worldPoint.x, transform.position.y, worldPoint.z)};
            lineRenderer.SetPositions(positions);
            lineRenderer.enabled = true;
        }

        private void Stop()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            isIdle = true;
        }

        private Vector3? CastMouseClickRay()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, rigidbody.position);

            // Debug Direction Ray
            //Debug.DrawRay(rigidbody.position, direction * 100, Color.red);

            float rayDistance;
            if (plane.Raycast(ray, out rayDistance))
            {
                // Debug Shot Ray
                Debug.DrawRay(rigidbody.position, ray.GetPoint(rayDistance), Color.yellow);

                return ray.GetPoint(rayDistance);
            }

            return null;
        }
    }

}
