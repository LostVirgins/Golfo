using UnityEngine;

namespace lv.gameplay
{
    public class EnterHole : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Hole"))
            {
                GameManager.Instance.PlayerInHole();
                //other.gameObject.GetComponent<Collider>().enabled = false;
                //other.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }
}
