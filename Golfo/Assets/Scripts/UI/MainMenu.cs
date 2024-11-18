using lv.network;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject MainMenuSec;
    public GameObject ConnectionSec;
    public GameObject HostServer;
    public GameObject JoinServer;
    public GameObject LobyViewSec;

    public TMP_InputField inputUsername;
    public TMP_InputField inputIP;
    public TMP_InputField inputLobyName;
    public TMP_InputField inputChatTxt;

    public GameObject NetworkManagerObj;
    public GameObject MessagePrefab;
    public GameObject ViewScrollContent;

    public TMP_Text LobyNameText;

    public string userName = "";
    public string lobyName = "Connected to - ";
    bool isHost = false;
    bool isChatting = false;

    private void Start()
    {
        
    }

    private void Update()
    {
        //if (Conections.GetComponent<ClientTCP>().lobyName != lobyName)
        //{
        //    lobyName = Conections.GetComponent<ClientTCP>().lobyName;
        //    LobyNameText.text = "Connected to - " + lobyName;
        //}

        if (isChatting)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                string message = $"{userName} - " + inputChatTxt.text;
                inputChatTxt.text = "";

                OnSendMessage(message);
            }
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
        lobyName = inputLobyName.text;
        LobyNameText.text = "Connected to - " + lobyName;

        NetworkManagerObj.GetComponent<NetworkManager>().StartHost();

        MainMenuSec.SetActive(false);
        LobyViewSec.SetActive(true);
        ConnectionSec.SetActive(true);
        HostServer.SetActive(false);
        isHost = true;
        isChatting = true;
    }

    public void JoinIP()
    {
        NetworkManagerObj.GetComponent<NetworkManager>().JoinServer(inputIP.text, userName);

        MainMenuSec.SetActive(false);
        LobyViewSec.SetActive(true);
        ConnectionSec.SetActive(true);
        JoinServer.SetActive(false);
        isChatting = true;
    }

    public void StartGame()
    {
        SceneManager.LoadScene(sceneName: "2_game_test");
    }

    public void ExitLoby()
    {
        if (isHost)
        {
            //TODO Shut Down server
        }
        else
        {
            //if (tcp.isOn)
            //    Conections.GetComponent<ClientTCP>().Disconnect();
            //else
            //    Conections.GetComponent<ClientUDP>().Disconnect();
        }

        MainMenuSec.SetActive(true);
        LobyViewSec.SetActive(false);
        isHost = false;
        isChatting = false;
    }

    public void OnSendMessage(string message)
    {
        InstantiateMessage(message);


        //if (isHost)
        //{
        //    if (tcp.isOn)
        //        Conections.GetComponent<ServerTCP>().BroadcastMessageServer(message, null);
        //    else
        //        Conections.GetComponent<ServerUDP>().BroadcastMessageServer(message, null);
        //}
        //else
        //{
        //    if (tcp.isOn)
        //        Conections.GetComponent<ClientTCP>().Send(message);
        //    else
        //        Conections.GetComponent<ClientUDP>().Send(message);
        //}
    }

    public void InstantiateMessage(string message)
    {
        GameObject messageObject = Instantiate(MessagePrefab, ViewScrollContent.transform);

        Debug.Log("Instaniated Object");

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
}
