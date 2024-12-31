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
            }
        }
    }
}
