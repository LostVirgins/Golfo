using lv.network;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Cinemachine;
using UnityEngine;

namespace lv.gameplay
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform m_target;

        [SerializeField] private Vector3 m_offset;

        [SerializeField] private float m_minDistance = 8f;
        [SerializeField] private float m_maxDistance = 45f;

        [SerializeField] private float m_xSpeed = 100f;
        [SerializeField] private float m_ySpeed = 70f;

        [SerializeField] private float m_yMinLimit = 15f;
        [SerializeField] private float m_yMaxLimit = 85f;

        private float x = 0f;
        private float y = 0f;
        private Vector3 position;
        private Quaternion rotation;
        float prevDistance;


        void Start()
        {
            //hekbas: comment next line to turn off camera easings
            //GetComponent<CinemachineVirtualCamera>().m_LookAt = m_target.transform;

            m_offset = transform.position - m_target.position;
            var angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;
        }

        void LateUpdate()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            float distance = m_offset.magnitude * (1f - scroll);
            distance = Mathf.Clamp(distance, m_minDistance, m_maxDistance);
            m_offset = m_offset.normalized * distance;

            if (Input.GetMouseButton(1)) // 1 for right mouse button
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                x += Input.GetAxis("Mouse X") * m_xSpeed * 0.02f;
                y -= Input.GetAxis("Mouse Y") * m_ySpeed * 0.02f;

                y = ClampAngle(y, m_yMinLimit, m_yMaxLimit);
                rotation = Quaternion.Euler(y, x, 0);
                transform.rotation = rotation;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            position = rotation * new Vector3(0.0f, 0.0f, -distance) + m_target.transform.position;
            transform.position = position;
            //m_offset = transform.position - m_target.position;

            if (Mathf.Abs(prevDistance - distance) > 0.001f)
            {
                prevDistance = distance;
                var rot = Quaternion.Euler(y, x, 0);
                var pos = rot * new Vector3(0.0f, 0.0f, -distance) + m_target.transform.position;
                transform.rotation = rot;
                transform.position = pos;
            }
        }

        static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360) angle += 360;
            if (angle >  360) angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
