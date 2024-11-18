using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bound : MonoBehaviour
{
    [SerializeField] private GameObject spawner;

    private void OnTriggerExit(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            collider.gameObject.transform.position = spawner.transform.position;
            collider.attachedRigidbody.velocity = Vector3.zero;
            collider.attachedRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
