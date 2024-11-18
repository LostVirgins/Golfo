using lv.network;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject MainMenuSec;
    public GameObject ConnectionSec;
    public GameObject HostServer;
    public GameObject JoinServer;
    public GameObject LobyViewSec;
    public GameObject Play;

    public TMP_InputField inputUsername;
    public TMP_InputField inputIP;
    public TMP_InputField inputLobyName;
    public TMP_InputField inputChatTxt;

    public GameObject NetworkManagerObj;
    public GameObject MessagePrefab;
    public GameObject ViewScrollContent;

    public TMP_Text LobyNameText;

    public string userName = "";
    public string lobbyName = "";
    bool isHost = false;
    bool isChatting = false;

    private void Start() { }

    private void Update()
    {
        if (isChatting)
        {
            if (Input.GetKeyDown(KeyCode.Return))
                OnSendMessage();
        }
    }

    public void ButtonHost()
    {
        userName = inputUsername.text;
        inputUsername.text = "";
        ConnectionSec.SetActive(false);
        HostServer.SetActive(true);
    }

    public void ButtonJoin()
    {
        userName = inputUsername.text;
        inputUsername.text = "";
        ConnectionSec.SetActive(false);
        JoinServer.SetActive(true);
    }

    public void StartHost()
    {
        lobbyName = inputLobyName.text;
        LobyNameText.text = "Lobby: " + lobbyName;

        NetworkManagerObj.GetComponent<NetworkManager>().StartHost(lobbyName);

        MainMenuSec.SetActive(false);
        LobyViewSec.SetActive(true);
        ConnectionSec.SetActive(true);
        HostServer.SetActive(false);
        Play.SetActive(true);
        isHost = true;
        isChatting = true;
    }

    public void JoinIP()
    {
        string serverIP = string.IsNullOrEmpty(inputIP.text) ? "127.0.0.1" : inputIP.text;
        NetworkManagerObj.GetComponent<NetworkManager>().JoinServer(serverIP, userName);

        MainMenuSec.SetActive(false);
        LobyViewSec.SetActive(true);
        ConnectionSec.SetActive(true);
        Play.SetActive(false);
        JoinServer.SetActive(false);
        isChatting = true;
    }

    public void StartGame()
    {
        Dictionary<IPEndPoint, Player> connectedPlayers = NetworkManager.Instance.m_connectedPlayers;

        Packet gameStart = new Packet();
        gameStart.WriteByte((byte)PacketType.game_start);
        gameStart.WriteString("hekbas_todo_use_token_:)");
        gameStart.WriteByte((byte)connectedPlayers.Count);

        foreach (var player in connectedPlayers)
        {
            gameStart.WriteString(player.Key.ToString());
            gameStart.WriteString(player.Value.m_sessionToken);
        }

        NetworkManager.Instance.m_sendQueue.Enqueue(new PacketData(gameStart, NetworkManager.Instance.m_serverEndPoint, true));

        SceneManager.LoadScene(sceneName: "2_game_test");
    }

    public void ExitLoby()
    {
        //hekbas: shutdown server

        MainMenuSec.SetActive(true);
        LobyViewSec.SetActive(false);
        isHost = false;
        isChatting = false;
    }

    public void OnSendMessage()
    {
        if (string.IsNullOrEmpty(inputChatTxt.text)) return;

        string message = $"{userName} - " + inputChatTxt.text;
        inputChatTxt.text = "";

        Packet chatMessage = new Packet(); 
        chatMessage.WriteByte((byte)PacketType.chat_message);
        chatMessage.WriteString("hekbas_todo_use_token_:)");
        chatMessage.WriteString(message);

        NetworkManager.Instance.m_sendQueue.Enqueue(new PacketData(chatMessage, NetworkManager.Instance.m_serverEndPoint));
    }

    public void DisplayMessage(string message)
    {
        InstantiateMessage(message);
    }

    public void InstantiateMessage(string message)
    {
        Debug.Log("Instaniated Message");

        GameObject messageObject = Instantiate(MessagePrefab, ViewScrollContent.transform);

        TextMeshProUGUI messageTextComponent = messageObject.GetComponentInChildren<TextMeshProUGUI>();
        if (messageTextComponent != null)
            messageTextComponent.text = message;

        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        RectTransform contentRect = ViewScrollContent.transform.GetComponent<RectTransform>();
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, 0);
    }

    public void SetLobbyName(string name)
    {
        lobbyName = name;
        LobyNameText.text = "Lobby: " + lobbyName;
    }

    private void OnEnable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnReceiveLobbyName.AddListener(SetLobbyName);
            NetworkManager.Instance.OnReceiveChatMessage.AddListener(DisplayMessage);
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnReceiveLobbyName.RemoveListener(SetLobbyName);
            NetworkManager.Instance.OnReceiveChatMessage.RemoveListener(DisplayMessage);
        }
    }
}
