using TMPro;
using UnityEngine;

namespace lv.ui
{
    public class UI_InGame : MonoBehaviour
    {
        public static UI_InGame Instance { get; private set; }

        public GameObject m_debugSec;
        public GameObject m_debugMessagePrefab;
        public GameObject m_debugViewContent;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Tab))
                TogleDebugWindow();
        }

    #region DebugWindow

        void TogleDebugWindow()
        {
            m_debugSec.SetActive(!m_debugSec.activeSelf);
        }

        public void DebugScreenLog(string message)
        {
            GameObject messageObject = Instantiate(m_debugMessagePrefab, m_debugViewContent.transform);

            TextMeshProUGUI messageTextComponent = messageObject.GetComponentInChildren<TextMeshProUGUI>();
            if (messageTextComponent != null)
                messageTextComponent.text = message;

            Canvas.ForceUpdateCanvases();

            RectTransform contentRect = m_debugViewContent.transform.GetComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0);
        }

    #endregion

    }
}
