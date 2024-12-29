using System.Collections;
using UnityEngine;

namespace lv.gameplay
{
    public class Bound : MonoBehaviour
    {
        [SerializeField] private GameObject spawner;
        [SerializeField] private float waitTime;
        private Collider playerCollider;

        private void OnTriggerExit(Collider collider)
        {
            if (collider.CompareTag("Player"))
            {
                playerCollider = collider;
                StartCoroutine(DelayedTP());
                Debug.Log("collision");
            }
        }

        private IEnumerator DelayedTP()
        {
            Debug.Log("Coroutine started...");

            yield return new WaitForSeconds(1.0f);

            playerCollider.gameObject.transform.position = playerCollider.gameObject.GetComponent<LineForce>().lastShotPosition;
            playerCollider.attachedRigidbody.velocity = Vector3.zero;
            playerCollider.attachedRigidbody.angularVelocity = Vector3.zero;
            Debug.Log("tp donete");
        }
    }
}
