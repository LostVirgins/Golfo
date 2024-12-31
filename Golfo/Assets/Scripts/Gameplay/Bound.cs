using System.Collections;
using UnityEngine;

namespace lv.gameplay
{
    public class Bound : MonoBehaviour
    {
        [SerializeField] private float waitTime = 1.0f;
        private Collider playerCollider;

        private void OnTriggerExit(Collider collider)
        {
            if (collider.CompareTag("Player"))
            {
                Debug.Log("Ball out of bounds");
                playerCollider = collider;
                StartCoroutine(DelayedTP());
            }
        }

        private IEnumerator DelayedTP()
        {
            yield return new WaitForSeconds(waitTime);

            playerCollider.gameObject.transform.position = playerCollider.gameObject.GetComponent<LineForce>().lastShotPosition;
            playerCollider.attachedRigidbody.velocity = Vector3.zero;
            playerCollider.attachedRigidbody.angularVelocity = Vector3.zero;
            Debug.Log("Teleported back to last position");
        }
    }
}
