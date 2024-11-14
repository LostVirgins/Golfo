using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace lv.network
{
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
        public PacketType AuthenticateClient(string username, IPEndPoint clientEndPoint, ref string sessionToken)
        {
            if (IsValidUser(username))
            {
                sessionToken = GenerateSessionToken(username);
                m_authenticatedSessions[clientEndPoint] = sessionToken;

                Packet responsePacket = new Packet();
                responsePacket.WriteInt((int)PacketType.auth_success);
                responsePacket.WriteString(sessionToken);
                NetworkManager.Instance.SendPacket(responsePacket, clientEndPoint);

                return PacketType.auth_success;
            }
            else
            {
                Packet responsePacket = new Packet();
                responsePacket.WriteInt((int)PacketType.auth_failure);
                NetworkManager.Instance.SendPacket(responsePacket, clientEndPoint);

                return PacketType.auth_failure;
            }
        }

        // Checks if the provided session token is valid for the client
        public PacketType IsAuthenticated(IPEndPoint clientEndPoint, string sessionToken)
        {
            if (m_authenticatedSessions.TryGetValue(clientEndPoint, out string validToken))
            {
                if (validToken == sessionToken)
                    return PacketType.auth_success;
                else
                    return PacketType.invalid_session;
            }
            else
            {
                return PacketType.expired_session;
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
