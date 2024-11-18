using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using TMPro;
using System.Collections.Concurrent;

public class ClientTCP : MonoBehaviour
{
    Socket server;
    Thread receiveThread;
    bool isConnected = false;

    public GameObject functionalities;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    string username;
    public string lobyName;

    void Start()
    {
        
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string message))
        {
            functionalities.GetComponent<Functionalities>().InstanciateMessage(message);
        }
    }

    public void StartClient(string ipAdress)
    {
        username = functionalities.GetComponent<Functionalities>().userName;
        if (isConnected)
        {
            Debug.Log("Already connected! Disconnect first.");
            return;
        }

        Thread connect = new Thread(() => Connect(ipAdress));
        connect.Start();
    }
    void Connect(string ipAdress)
    {
        try
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ipAdress), 9050);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Connect(ipep);
            isConnected = true;

            Send(username);

            byte[] data = new byte[1024];
            int recv = server.Receive(data);

            if (recv > 0)
            {
                string serverName = Encoding.ASCII.GetString(data, 0, recv);
                lobyName = serverName;
            }
            else
            {
                Debug.LogWarning("No servername received from the host.");
                lobyName = "Unknown Loby";
            }

            receiveThread = new Thread(Receive);
            receiveThread.Start();
        }
        catch (SocketException socketEx)
        {
            Debug.LogError($"SocketException: {socketEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception during Connect: {ex.Message}");
        }
    }
    public void Send(string message)
    {
        try
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            server.Send(buffer);
            Debug.Log($"Message sent: {message}");
        }
        catch (SocketException socketEx)
        {
            Debug.LogError($"SocketException during Send: {socketEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception during Send: {ex.Message}");
        }
    }

    void Receive()
    {
        try
        {
            while (isConnected)
            {
                byte[] data = new byte[1024];
                int recv = server.Receive(data);

                if (recv == 0)
                {
                    Debug.Log("Server closed the connection.");
                    break;
                }

                string receivedMessage = Encoding.ASCII.GetString(data, 0, recv);

                messageQueue.Enqueue(receivedMessage);
            }
        }
        catch (SocketException socketEx)
        {
            Debug.LogError($"SocketException during Receive: {socketEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception during Receive: {ex.Message}");
        }

    }

    public void Disconnect()
    {
        try
        {
            isConnected = false;
            if (server != null)
            {
                server.Shutdown(SocketShutdown.Both);
                server.Close();
                Debug.Log("Disconnected from server.");
            }
        }
        catch (SocketException socketEx)
        {
            Debug.LogError($"SocketException during Disconnect: {socketEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception during Disconnect: {ex.Message}");
        }
    }
}
