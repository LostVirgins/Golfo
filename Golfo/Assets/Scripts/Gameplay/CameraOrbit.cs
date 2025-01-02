using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    public Transform target;
    public float orbitSpeed = 10f;
    public float distance = 5f;
    public Vector3 offset = Vector3.zero;

    private float currentAngle = 0f;

    void Update()
    {
        if (target == null) return;

        currentAngle += orbitSpeed * Time.deltaTime;

        float radians = currentAngle * Mathf.Deg2Rad;
        float x = target.position.x + offset.x + Mathf.Cos(radians) * distance;
        float z = target.position.z + offset.z + Mathf.Sin(radians) * distance;
        float y = target.position.y + offset.y; // Keep the height fixed

        transform.position = new Vector3(x, y, z);
        transform.LookAt(target.position + offset);
    }
}
