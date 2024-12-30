using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace lv.gameplay
{
    public class UI_InGame : MonoBehaviour
    {

        public GameObject m_debugSec;
        public GameObject m_debugMessagePrefab;
        public GameObject m_debugViewContent;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.F1))
                TogleDebugWindow();

            if (Input.GetKeyDown(KeyCode.F2))
            {
                DebugScreenLog("hekbai putelo");
            }
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