using lv.network;
using Unity.VisualScripting;
using System.Net;
using UnityEngine;
using lv.ui;
using UnityEngine.UIElements;

namespace lv.gameplay
{
    public class LineForce : MonoBehaviour
    {
        [SerializeField] private float shotPower;
        [SerializeField] private float stopVelocity;
        [SerializeField] private float minShotDistance;
        [SerializeField] private float maxForce;
        [SerializeField] private float frequency = 1f;

        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private LineRenderer lineRendererArrow;

        private float force;
        private Vector3 aimDirection;

        private float lastSpeed;
        private bool isDecelerating;
        private bool isIdle;
        private bool isAiming;
        private float aimRNG;

        private Rigidbody rigidbody;

        Ray ray;
        RaycastHit hit;
        public Vector3 lastShotPosition { get; set; }

        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();

            isAiming = false;
            lineRenderer.enabled = false;
        }

        private void Update()
        {
            isDecelerating = lastSpeed >= rigidbody.velocity.magnitude ? true : false;
            lastSpeed = rigidbody.velocity.magnitude;

            if (isDecelerating && rigidbody.velocity.magnitude < stopVelocity)
            {
                isIdle = true;
                rigidbody.drag = 2.5f;
                rigidbody.angularDrag = 2.5f;
            }
            else
            {
                isIdle = false;
                rigidbody.drag = 0.6f;
                rigidbody.angularDrag = 0.6f;
            }

            if (Input.GetMouseButtonDown(0))
            {
                int playerBallLayerMask = LayerMask.GetMask("PlayerBall");
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, playerBallLayerMask))
                    if (isIdle) isAiming = true;
            }

            ProcessAim();
        }

        private void ProcessAim()
        {
            if (!isAiming || !isIdle) return;
            Vector3? worldPoint = CastMouseClickRay();
            if (!worldPoint.HasValue) return;

            force = Vector3.Distance(rigidbody.transform.position, worldPoint.Value);
            Vector3 forceIndicator = GetClampedForceIndicator(worldPoint.Value);
            Vector3 aimDirection = GetClampedAimDirection(worldPoint.Value);
            Vector3 aimRNG = transform.position + (AimRNG(aimDirection.normalized) * GetClampedForce());

            DrawForceIndicator(forceIndicator);
            DrawArrowIndicator(aimRNG);

            if (Input.GetMouseButtonUp(0))
                Shoot(aimRNG);
        }

        private float GetClampedForce()
        {
            return Mathf.Min(force, maxForce);
        }

        private Vector3 GetClampedForceIndicator(Vector3 worldPoint)
        {
            Vector3 forceIndicator = worldPoint - transform.position;
            return transform.position + Vector3.ClampMagnitude(forceIndicator, maxForce);
        }

        private Vector3 GetClampedAimDirection(Vector3 worldPoint)
        {
            Vector3 aimDirection = transform.position - worldPoint;
            return Vector3.ClampMagnitude(aimDirection, maxForce);
        }

        private Vector3 AimRNG(Vector3 aimDirection)
        {
            if (force > maxForce / 3)
                return OscillateVector(aimDirection);

            aimRNG = 0;
            return aimDirection;
        }

        public Vector3 OscillateVector(Vector3 input)
        {
            if (input == Vector3.zero) return Vector3.zero;

            aimRNG += Time.deltaTime * GetClampedForce() * 0.5f;
            float maxAngle = Mathf.Clamp(7f * GetClampedForce(), 10, 40);
            float oscillationAngle = Mathf.Sin(aimRNG * frequency) * maxAngle;

            return RotateVector(input.normalized, oscillationAngle).normalized;
        }

        private Vector3 RotateVector(Vector3 vector, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector3(
                vector.x * cos - vector.z * sin,
                vector.y,
                vector.x * sin + vector.z * cos
            );
        }

        private void Shoot(Vector3 aimDirection)
        {
            isAiming = false;
            lineRenderer.enabled = false;
            lineRendererArrow.enabled = false;

            if (minShotDistance > force) return;

            //save last pos as checkpoint when falling out a course
            lastShotPosition = transform.position;

            Vector3 horizontalWorldPoint = new Vector3(aimDirection.x, transform.position.y, aimDirection.z);
            Vector3 direction = (horizontalWorldPoint - transform.position).normalized;
            float strength = GetClampedForce();

            rigidbody.AddForce(direction * strength * shotPower);

            // Notify Server for input prediction
            Packet packet = new Packet();
            packet.WriteByte((byte)PacketType.ball_strike);
            packet.WriteString("hekbas_todo_use_token_:)");
            packet.WriteString(NetworkManager.Instance.m_localEndPoint.ToString());

            NetworkManager.Instance.EnqueueSend(new PacketData(packet, NetworkManager.Instance.m_hostEndPoint));
        }

        private void DrawForceIndicator(Vector3 forceIndicator)
        {
            Vector3[] positions = {
            new Vector3(transform.position.x, transform.position.y, transform.position.z),
            new Vector3(forceIndicator.x, transform.position.y, forceIndicator.z)};
            lineRenderer.SetPositions(positions);
            lineRenderer.enabled = true;

            // Set color
            float lineColorLength = force / maxForce;
            Color currentLineColor;
            if (lineColorLength <= 0.5f)
                currentLineColor = Color.Lerp(Color.green, Color.yellow, lineColorLength / 0.5f);
            else
                currentLineColor = Color.Lerp(Color.yellow, Color.red, (lineColorLength - 0.5f) / 0.5f);

            lineRenderer.startColor = currentLineColor;
            lineRenderer.endColor = currentLineColor;
        }

        private void DrawArrowIndicator(Vector3 aimDirection)
        {
            Vector3[] positionsArrow = {
                new Vector3(transform.position.x, transform.position.y, transform.position.z),
                new Vector3(aimDirection.x, transform.position.y, aimDirection.z)
            };
            lineRendererArrow.SetPositions(positionsArrow);
            lineRendererArrow.enabled = true;
        }

        private Vector3? CastMouseClickRay()
        {
            if (hit.collider.gameObject == GameManager.Instance.m_player)
            {
                var plane = new Plane(Vector3.up, GameManager.Instance.m_player.GetComponent<Rigidbody>().position);
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                float rayDistance;
                if (plane.Raycast(ray, out rayDistance))
                    return ray.GetPoint(rayDistance);
            }

            return null;
        }
    }
}
