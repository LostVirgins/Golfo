using UnityEngine;

namespace lv.gameplay
{
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
}
