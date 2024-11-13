using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;
using System.Text;
using UnityEditor;
using System.Collections.Generic;
using static ServerTCP;
using System.Collections.Concurrent;

public class ServerTCP : MonoBehaviour
{
    Socket socket;
    Thread mainThread = null;

    public GameObject functionalities;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    string username;
    string lobyName;

    public class User
    {
        public string name;
        public Socket socket;
    }

    List<User> connectedUsers = new List<User>();

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


    public void startServer(string lobyName)
    {
        this.lobyName = lobyName;
        username = functionalities.GetComponent<Functionalities>().userName;

        try
        {
            Debug.Log("Starting TCP Server...");

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 9050);

            socket.Bind(endPoint);
            socket.Listen(10);

            Debug.Log("Server started. Listening for connections...");

            mainThread = new Thread(CheckNewConnections);
            mainThread.Start();
        }
        catch (SocketException socketEx)
        {
            Debug.LogError($"SocketException in startServer: {socketEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception in startServer: {ex.Message}");
        }
    }

    void CheckNewConnections()
    {
        Debug.Log("Waiting for new Client...");

        while (true)
        {
            try
            {
                User newUser = new User();
                newUser.socket = socket.Accept();

                byte[] data = new byte[1024];
                int recv = newUser.socket.Receive(data);

                if (recv > 0)
                {
                    newUser.name = Encoding.ASCII.GetString(data, 0, recv);
                }
                else
                {
                    Debug.LogWarning("No username received from the client.");
                    newUser.name = "Unknown User";
                }

                lock (connectedUsers)
                {
                    connectedUsers.Add(newUser);
                }

                Send(lobyName, newUser);

                BroadcastMessageServer($"{newUser.name} is now connected.", null);
                messageQueue.Enqueue($"{newUser.name} is now connected.");

                IPEndPoint clientep = (IPEndPoint)newUser.socket.RemoteEndPoint;

                Thread newConnection = new Thread(() => Receive(newUser));
                newConnection.Start();
            }
            catch (SocketException socketEx)
            {
                Debug.LogError($"SocketException in CheckNewConnections: {socketEx.Message}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception in CheckNewConnections: {ex.Message}");
            }
        }
    }

    void Receive(User user)
    {
        byte[] data = new byte[1024];
        int recv = 0;

        try
        {
            while (true)
            {
                data = new byte[1024];
                recv = user.socket.Receive(data);

                if (recv == 0)
                    break;

                string receivedMessage = Encoding.ASCII.GetString(data, 0, recv);
                messageQueue.Enqueue(receivedMessage);

                BroadcastMessageServer(receivedMessage, user);
            }

            lock (connectedUsers)
            {
                BroadcastMessageServer($"{user.name} has disconnected.", user);
                messageQueue.Enqueue($"{user.name} has disconnected.");
                connectedUsers.Remove(user);
            }

            user.socket.Close();
        }
        catch (SocketException socketEx)
        {
            Debug.LogError($"SocketException in Receive: {socketEx.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception in Receive: {ex.Message}");
        }
    }

    public void BroadcastMessageServer(string message, User sender)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(message);

        lock (connectedUsers)
        {
            foreach (User user in connectedUsers)
            {
                if ((sender == null) || (user.socket != sender.socket))
                {
                    user.socket.Send(buffer);
                }
            }
        }
    }

    void Send(string message, User user)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(message);

        user.socket.Send(buffer);
    }
}
