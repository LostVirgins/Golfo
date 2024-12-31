using lv.network;
using Unity.VisualScripting;
using System.Net;
using UnityEngine;
using lv.ui;

namespace lv.gameplay
{
    public class LineForce : MonoBehaviour
    {
        [SerializeField] private float shotPower;
        [SerializeField] private float stopVelocity;
        [SerializeField] private float minShotDistance;
        [SerializeField] private float maxShotDistance;

        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private LineRenderer lineRendererArrow;

        private float distanceFromBall;

        private float lastSpeed;
        private bool isDecelerating;
        private bool isIdle;
        private bool isAiming;

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

            distanceFromBall = Vector3.Distance(rigidbody.transform.position, worldPoint.Value);

            Vector3 directionFromBall = worldPoint.Value - transform.position;
            Vector3 clampedLineDirectionFromBall = transform.position + Vector3.ClampMagnitude(directionFromBall, maxShotDistance);
            Vector3 invertedDirectionFromBall = directionFromBall * -1;
            Vector3 invertedLineDirectionFromBall = transform.position + Vector3.ClampMagnitude(invertedDirectionFromBall, maxShotDistance);

            DrawLine(worldPoint.Value, clampedLineDirectionFromBall, invertedLineDirectionFromBall);

            if (Input.GetMouseButtonUp(0))
                Shoot(worldPoint.Value, clampedLineDirectionFromBall);
        }

        private void Shoot(Vector3 worldPoint, Vector3 clampedLineDirectionFromBall)
        {
            isAiming = false;
            lineRenderer.enabled = false;
            lineRendererArrow.enabled = false;

            UI_InGame.Instance.DebugScreenLog("IS AIMING FALSE -------");

            //check if shot possible
            if (minShotDistance > distanceFromBall) return;

            //save last pos as checkpoint when falling out a course
            lastShotPosition = transform.position;

            Vector3 horizontalWorldPoint = new Vector3(clampedLineDirectionFromBall.x, transform.position.y, clampedLineDirectionFromBall.z);
            Vector3 direction = (horizontalWorldPoint - transform.position).normalized;
            float strength = Vector3.Distance(transform.position, horizontalWorldPoint);

            rigidbody.AddForce(-direction * strength * shotPower);

            // Notify Server for input prediction
            Packet packet = new Packet();
            packet.WriteByte((byte)PacketType.ball_strike);
            packet.WriteString("hekbas_todo_use_token_:)");
            packet.WriteString(NetworkManager.Instance.m_localEndPoint.ToString());
            packet.WriteVector3(direction);
            packet.WriteFloat(strength);
            packet.WriteFloat(shotPower);

            NetworkManager.Instance.EnqueueSend(new PacketData(packet, NetworkManager.Instance.m_hostEndPoint));
        }

        private void DrawLine(Vector3 worldPoint, Vector3 clampedLineDirectionFromBall, Vector3 invertedLineDirectionFromBall)
        {
            //draw lineRenderer
            Vector3[] positions = {
            new Vector3(transform.position.x, transform.position.y, transform.position.z),
            new Vector3(clampedLineDirectionFromBall.x, transform.position.y, clampedLineDirectionFromBall.z)};
            lineRenderer.SetPositions(positions);
            lineRenderer.enabled = true;

            // Set color
            float lineColorLength = distanceFromBall / maxShotDistance;
            Color currentLineColor;
            if (lineColorLength <= 0.5f)
                currentLineColor = Color.Lerp(Color.green, Color.yellow, lineColorLength / 0.5f);
            else
                currentLineColor = Color.Lerp(Color.yellow, Color.red, (lineColorLength - 0.5f) / 0.5f);

            lineRenderer.startColor = currentLineColor;
            lineRenderer.endColor = currentLineColor;

            //Debug.Log( "Distance -->" + distanceFromBall + " & >>> " + worldPoint + " <<<");

            //draw lineRendererArrow
            Vector3[] positionsArrow = {
                new Vector3(transform.position.x, transform.position.y, transform.position.z),
                new Vector3(invertedLineDirectionFromBall.x, transform.position.y, invertedLineDirectionFromBall.z)
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
