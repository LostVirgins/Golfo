using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Sprites;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    private UdpClient udpClient;
    private Dictionary<IPEndPoint, Player> connectedPlayers = new Dictionary<IPEndPoint, Player>();
    private Dictionary<IPEndPoint, string> clientSessions = new Dictionary<IPEndPoint, string>();

    private IPEndPoint serverEndPoint;

    private void Awake()
    {
        Instance = this;
        udpClient = new UdpClient(9050);
        udpClient.BeginReceive(OnReceiveData, null);
    }

    public void StartHost()
    {
        serverEndPoint = null;
        Debug.Log("Hosting game...");
    }

    public void JoinServer(string ipAddress)
    {
        serverEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 9050);
        Debug.Log("Joining server...");
    }

    private void OnReceiveData(IAsyncResult result)
    {
        byte[] data = udpClient.EndReceive(result, ref serverEndPoint);
        Packet packet = new Packet(data);

        int packetType = packet.ReadInt();

        if (packetType == 0) // 0 = connection request (authentication)
        {
            AuthenticationStatus status = AuthenticationManager.Instance.AuthenticateClient(packet, serverEndPoint);

            if (status == AuthenticationStatus.Success)
            {
                string sessionToken = packet.ReadString();
                clientSessions[serverEndPoint] = sessionToken;

                Player newPlayer = new Player(sessionToken);
                connectedPlayers[serverEndPoint] = newPlayer;

                Debug.Log($"Player authenticated with session {sessionToken}");
            }
            else
            {
                Debug.Log("Authentication failed.");
            }
        }
        else
        {
            string clientSessionToken = packet.ReadString();

            AuthenticationStatus authStatus = AuthenticationManager.Instance.IsAuthenticated(serverEndPoint, clientSessionToken);

            switch (authStatus)
            {
                case AuthenticationStatus.Success:
                    ProcessGamePacket(packet, serverEndPoint);
                    break;

                case AuthenticationStatus.InvalidSession:
                    Debug.Log("Received packet with an invalid session token.");
                    break;

                case AuthenticationStatus.SessionExpired:
                    Debug.Log("Received packet from a client with an expired session.");
                    break;

                default:
                    Debug.Log("Unknown authentication status.");
                    break;
            }
        }

        udpClient.BeginReceive(OnReceiveData, null);
    }

    private void ProcessGamePacket(Packet packet, IPEndPoint clientEndPoint)
    {
        Debug.Log("Processing game packet...");
    }

    public void SendPacket(Packet packet, IPEndPoint endPoint = null)
    {
        endPoint = endPoint ?? serverEndPoint;
        byte[] data = packet.GetData();
        udpClient.Send(data, data.Length, endPoint);
    }

    private void OnApplicationQuit()
    {
        udpClient.Close();
    }
}
