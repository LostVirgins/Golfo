using lv.gameplay;
using lv.network;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

namespace lv.ui
{
    public class UI_InGame : MonoBehaviour
    {
        public static UI_InGame Instance { get; private set; }

        public GameObject m_scoreSec;
        public GameObject m_scoreNodePrefab;
        public GameObject m_scoreViewContent;

        public GameObject m_debugSec;
        public GameObject m_debugMessagePrefab;
        public GameObject m_debugViewContent;

        public GameObject m_exitButton;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Tab))
                ToggleDebugWindow();
        }

        #region ScoreWindow

        public void ToggleScoreWindow()
        {
            if (GameManager.Instance.m_gameState == GameState.playing)
            {
                m_scoreSec.SetActive(!m_scoreSec.activeSelf);

                if (m_scoreSec.activeSelf)
                    OpenScoreWindow();
                else
                    CloseScoreWindow();
            }

            if (GameManager.Instance.m_gameState == GameState.game_end)
            {
                m_scoreSec.SetActive(true);
                CloseScoreWindow();
                OpenScoreWindow();
            }
        }

        void OpenScoreWindow()
        {
            List<Player> playersByTotalScore = NetworkManager.Instance.m_players.Values.OrderBy(player => player.GetTotalScore()).ToList();
            foreach (var player in playersByTotalScore)
                AddScoreNode(player);
        }

        void CloseScoreWindow()
        {
            List<GameObject> children = GameManager.Instance.GetAllChildren(m_scoreViewContent);
            foreach (var child in children)
                Destroy(child);
        }

        void AddScoreNode(Player player)
        {
            GameObject messageObject = Instantiate(m_scoreNodePrefab, m_scoreViewContent.transform);
            List<GameObject> children = GameManager.Instance.GetAllChildren(messageObject);

            children[0].GetComponent<TextMeshProUGUI>().text = player.m_username;

            for (int i = 1; i < GameManager.Instance.m_currentHole + 2; i++)
            {
                int score = player.m_score[i - 1];
                children[i].GetComponent<TextMeshProUGUI>().text = score.ToString();
                children[i].GetComponent<TextMeshProUGUI>().color = SetScoreColor(score, GameManager.Instance.GetHolePar(i-1));
            }

            int totalScore = player.GetTotalScore() - GameManager.Instance.CurrentParSum();
            children[7].GetComponent<TextMeshProUGUI>().text = totalScore.ToString();
            children[7].GetComponent<TextMeshProUGUI>().color = SetScoreColor(player.GetTotalScore(), GameManager.Instance.CurrentParSum());

            Canvas.ForceUpdateCanvases();
        }

        Color SetScoreColor(int score, int par)
        {
            int result = score - par;
            switch (result)
            {
                case 0:     return Color.black;
                case > 0:   return Color.red;
                case < 0:   return Color.green;
            }
        }

        #endregion

        #region DebugWindow

        void ToggleDebugWindow()
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

        public void ToggleExit()
        {
            m_exitButton.SetActive(!m_exitButton.activeSelf);
        }

        public void OnExit()
        {
            if (NetworkManager.Instance.m_isHost)
                NetworkManager.Instance.ShutdownHost();
            else
                NetworkManager.Instance.ExitServer();

            NetworkManager.Instance.RemoveOnLoad();
            SceneManager.LoadScene(sceneName: "1_main_menu");
        }
    }
}
