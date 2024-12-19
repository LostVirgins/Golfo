using lv.network;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace lv.gameplay
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;

        Vector3 offset;

        public float rotationSpeed = 200f;
        public float xSpeed = 200f;
        public float ySpeed = 200f;
        public float minDistance = 8f;
        public float maxDistance = 45f;
        public float smoothSpeed = 10f;
        public float x = 0.0f;
        public float y = 0.0f;

        public float yMinLimit = 30;
        public float yMaxLimit = 50;


        void Start()
        {
            offset = transform.position - target.position;
            var angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;
        }

        void Update()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            float distance = offset.magnitude * (1f - scroll);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            offset = offset.normalized * distance;

            if (Input.GetMouseButton(1)) // 1 for right mouse button
            {
                x += Input.GetAxis("Mouse X") * 50 * 0.02f;
                y -= Input.GetAxis("Mouse Y") * 50 * 0.02f;

                y = ClampAngle(y, yMinLimit, yMaxLimit);
                var rotation = Quaternion.Euler(y, x, 0);
                var position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.transform.position;
                transform.rotation = rotation;
                transform.position = position;

                offset = transform.position - target.position;
            }

            // Maintain the camera's position relative to the ball
            Vector3 desiredPosition = target.position + offset;
            
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }

        static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360)
                angle += 360;
            if (angle > 360)
                angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }
    }

}

