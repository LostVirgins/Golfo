using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Functionalities : MonoBehaviour
{
    public Toggle tcp;
    public Toggle udp;

    public GameObject MainMenuSec;
    public GameObject LoginSec;
    public GameObject ConnectionSec;
    public GameObject InputIpSec;
    public GameObject InputLobyNameSec;
    public GameObject LobyViewSec;

    public TMP_InputField inputUsername;
    public TMP_InputField inputIP;
    public TMP_InputField inputLobyName;
    public TMP_InputField inputChatTxt;

    public GameObject Conections;
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
        if (!isHost)
        {
            if (tcp.isOn)
            {
                if (Conections.GetComponent<ClientTCP>().lobyName != lobyName)
                {
                    lobyName = Conections.GetComponent<ClientTCP>().lobyName;
                    LobyNameText.text = "Connected to - " + lobyName;
                }
            }
            else
            {
                if (Conections.GetComponent<ClientUDP>().lobyName != lobyName)
                {
                    lobyName = Conections.GetComponent<ClientUDP>().lobyName;
                    LobyNameText.text = "Connected to - " + lobyName;
                }
            }
        }

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

    public void SetUserName()
    {
        userName = inputUsername.text;
        inputUsername.text = "";
        LoginSec.SetActive(false);
        ConnectionSec.SetActive(true);
    }

    public void ButtonHost()
    {
        ConnectionSec.SetActive(false);
        InputLobyNameSec.SetActive(true);
    }

    public void ButtonJoin()
    {
        ConnectionSec.SetActive(false);
        InputIpSec.SetActive(true);
    }

    public void StartHost()
    {
        lobyName = inputLobyName.text;
        LobyNameText.text = "Connected to - " + lobyName;

        if (tcp.isOn)
            Conections.GetComponent<ServerTCP>().startServer(lobyName);
        else
            Conections.GetComponent<ServerUDP>().startServer(lobyName);

        MainMenuSec.SetActive(false);
        LobyViewSec.SetActive(true);
        ConnectionSec.SetActive(true);
        InputLobyNameSec.SetActive(false);
        isHost = true;
        isChatting = true;
    }

    public void JoinIP()
    {
        if (tcp.isOn)
            Conections.GetComponent<ClientTCP>().StartClient(inputIP.text);
        else
            Conections.GetComponent<ClientUDP>().StartClient(inputIP.text);

        MainMenuSec.SetActive(false);
        LobyViewSec.SetActive(true);
        ConnectionSec.SetActive(true);
        InputIpSec.SetActive(false);
        isChatting = true;
    }

    public void ExitLoby()
    {
        if (isHost)
        {
            //TODO Shut Down server
        }
        else
        {
            if (tcp.isOn)
                Conections.GetComponent<ClientTCP>().Disconnect();
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
        InstanciateMessage(message);


        if (isHost)
        {
            if (tcp.isOn)
                Conections.GetComponent<ServerTCP>().BroadcastMessageServer(message, null);
            else
                Conections.GetComponent<ServerUDP>().BroadcastMessageServer(message, null);
        }
        else
        {
            if (tcp.isOn)
                Conections.GetComponent<ClientTCP>().Send(message);
            else
                Conections.GetComponent<ClientUDP>().Send(message);
        }
    }

    public void InstanciateMessage(string message)
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
