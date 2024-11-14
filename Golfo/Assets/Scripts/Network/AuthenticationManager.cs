using System.Collections.Generic;
using System.Net;
using UnityEngine;

public enum AuthenticationStatus
{
    Success,
    Failure,
    InvalidSession,
    SessionExpired
}

public class AuthenticationManager : MonoBehaviour
{
    private Dictionary<IPEndPoint, string> authenticatedSessions = new Dictionary<IPEndPoint, string>();
    private HashSet<string> validUsers = new HashSet<string> { "player1", "player2" };

    public static AuthenticationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Authenticates the client and returns the result as an AuthenticationStatus
    public AuthenticationStatus AuthenticateClient(Packet packet, IPEndPoint clientEndPoint)
    {
        string username = packet.ReadString();

        if (IsValidUser(username))
        {
            string sessionToken = GenerateSessionToken(username);
            authenticatedSessions[clientEndPoint] = sessionToken;

            // Send a success packet back with the session token
            Packet responsePacket = new Packet();
            responsePacket.WriteInt((int)AuthenticationStatus.Success);
            responsePacket.WriteString(sessionToken);
            NetworkManager.Instance.SendPacket(responsePacket, clientEndPoint);

            return AuthenticationStatus.Success;
        }
        else
        {
            // Send a failure packet back to the client
            Packet responsePacket = new Packet();
            responsePacket.WriteInt((int)AuthenticationStatus.Failure);
            NetworkManager.Instance.SendPacket(responsePacket, clientEndPoint);

            return AuthenticationStatus.Failure;
        }
    }

    // Checks if the provided session token is valid for the client
    public AuthenticationStatus IsAuthenticated(IPEndPoint clientEndPoint, string sessionToken)
    {
        if (authenticatedSessions.TryGetValue(clientEndPoint, out string validToken))
        {
            if (validToken == sessionToken)
            {
                return AuthenticationStatus.Success;
            }
            else
            {
                return AuthenticationStatus.InvalidSession;
            }
        }
        else
        {
            return AuthenticationStatus.SessionExpired;
        }
    }

    private string GenerateSessionToken(string username)
    {
        return $"{username}_{System.Guid.NewGuid()}";
    }

    private bool IsValidUser(string username)
    {
        return validUsers.Contains(username);
    }

    public void RemoveSession(IPEndPoint clientEndPoint)
    {
        if (authenticatedSessions.ContainsKey(clientEndPoint))
        {
            authenticatedSessions.Remove(clientEndPoint);
        }
    }
}
