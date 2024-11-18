using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class HoleInOne : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Collider>().enabled = false;
            other.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}
