using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace lv.network
{
    public enum AuthenticationStatus
    {
        Success,
        Failure,
        InvalidSession,
        SessionExpired
    }

    public class AuthenticationManager : MonoBehaviour
    {
        public static AuthenticationManager Instance { get; private set; }

        private Dictionary<IPEndPoint, string> m_authenticatedSessions = new Dictionary<IPEndPoint, string>();
        private HashSet<string> m_validUsers = new HashSet<string> { "hekbas", "kikofp02", "IITROSDASEII", "punto16", "chu3rk" };


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
                m_authenticatedSessions[clientEndPoint] = sessionToken;

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
            if (m_authenticatedSessions.TryGetValue(clientEndPoint, out string validToken))
            {
                if (validToken == sessionToken)
                    return AuthenticationStatus.Success;
                else
                    return AuthenticationStatus.InvalidSession;
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
            return m_validUsers.Contains(username);
        }

        public void RemoveSession(IPEndPoint clientEndPoint)
        {
            if (m_authenticatedSessions.ContainsKey(clientEndPoint))
            {
                m_authenticatedSessions.Remove(clientEndPoint);
            }
        }
    }
}
