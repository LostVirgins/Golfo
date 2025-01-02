using lv.ui;
using System.Collections;
using UnityEngine;

namespace lv.gameplay
{
    public class Bound : MonoBehaviour
    {
        [SerializeField] private float waitTime = 1.5f;

        private void OnTriggerExit(Collider collider)
        {
            if (collider.CompareTag("MyPlayer"))
                StartCoroutine(DelayedTP());
        }

        private IEnumerator DelayedTP()
        {
            yield return new WaitForSeconds(waitTime);
            GameManager.Instance.m_gameState = GameState.out_of_bounds;
            UI_InGame.Instance.DebugScreenLog("Ball out of bounds");
        }
    }
}
