using lv.gameplay;
using lv.network;
using System.Collections.Generic;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;

namespace lv.ui
{
    public class UI_InGame : MonoBehaviour
    {
        public static UI_InGame Instance { get; private set; }

        NetworkManager networkManager = NetworkManager.Instance;
        GameManager gameManager = GameManager.Instance;

        public GameObject m_scoreSec;
        public GameObject m_scoreNodePrefab;
        public GameObject m_scoreViewContent;

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

        #region ScoreWindow

        public void TogleScoreWindow()
        {
            m_scoreSec.SetActive(!m_debugSec.activeSelf);

            if(m_scoreSec.activeSelf)
                OpenScoreWindow();
            else
                CloseScoreWindow();
        }

        void CloseScoreWindow()
        {
            // borrar todos los children de m_scoreViewContent
        }

        void OpenScoreWindow()
        {
            // Esto esta mal, faltan pasos
                    //Primero ordenar la lista de players por score, luego añadir los node por cada player
            foreach (var player in networkManager.m_players)
            {
                AddScoreNode(player);
            }
        }


        void AddScoreNode(Player player)
        {
            GameObject messageObject = Instantiate(m_debugMessagePrefab, m_debugViewContent.transform);

            List<GameObject> children = gameManager.GetAllChildren(messageObject);

            int i = 0;
            foreach (GameObject child in children)
            {
                TextMeshProUGUI messageTextComponent = child.GetComponent<TextMeshProUGUI>();
                if (messageTextComponent != null)
                {
                    if (i == 0)
                    {
                        messageTextComponent.text = player.name;
                    }
                    else if(i == 7)
                    {
                        // Total Score
                    }
                    else
                    {
                        messageTextComponent.text = player.m_score[i--];
                    }
                }

                ++i;
            }

            Canvas.ForceUpdateCanvases();
        }

        #endregion

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
