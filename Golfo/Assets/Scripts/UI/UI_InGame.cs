using lv.gameplay;
using lv.network;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
            m_scoreSec.SetActive(!m_scoreSec.activeSelf);

            if (m_scoreSec.activeSelf)
                OpenScoreWindow();
            else
                CloseScoreWindow();
        }

        void OpenScoreWindow()
        {
            List<Player> playersByTotalScore = networkManager.m_players.Values.OrderBy(player => player.GetTotalScore()).ToList();
            DebugScreenLog(playersByTotalScore.Count.ToString());
            foreach (var player in playersByTotalScore)
            {
                DebugScreenLog("+");
                AddScoreNode(player);
            }
        }

        void CloseScoreWindow()
        {
            List<GameObject> children = GameManager.Instance.GetAllChildren(m_scoreViewContent);
            foreach (var child in children)
            {
                Destroy(child);
                DebugScreenLog("-");
            }
        }

        void AddScoreNode(Player player)
        {
            GameObject messageObject = Instantiate(m_scoreNodePrefab, m_scoreViewContent.transform);
            List<GameObject> children = GameManager.Instance.GetAllChildren(messageObject);

            children[0].GetComponent<TextMeshProUGUI>().text = player.m_username;
            children[7].GetComponent<TextMeshProUGUI>().text = player.GetTotalScore().ToString();

            for (int i = 1; i < GameManager.Instance.currentHole + 2; i++)
                children[i].GetComponent<TextMeshProUGUI>().text = player.m_score[i - 1].ToString();

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
