using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace lv.network
{
    public class AuthenticationManager : MonoBehaviour
    {
        public static AuthenticationManager Instance { get; private set; }

        public Dictionary<IPEndPoint, string> m_authenticatedSessions { get; private set; } = new Dictionary<IPEndPoint, string>();
        private HashSet<string> m_validUsers = new HashSet<string> { "hekbas", "itsNick02", "IITROSDASEII", "punto16", "chu3rk" };


        private void Awake()
        {
            Instance = this;
        }

        // Authenticates the client and returns the result as a PacketType
        public PacketType AuthenticateClient(string username, IPEndPoint clientEndPoint, ref string sessionToken)
        {
            if (IsValidUser(username))
            {
                sessionToken = GenerateSessionToken();
                m_authenticatedSessions[clientEndPoint] = sessionToken;

                Packet responsePacket = new Packet();
                responsePacket.WriteByte((byte)PacketType.auth_success);
                responsePacket.WriteString(sessionToken);
                NetworkManager.Instance.m_sendQueue.Enqueue(new PacketData(responsePacket, clientEndPoint));

                return PacketType.auth_success;
            }
            else
            {
                Packet responsePacket = new Packet();
                responsePacket.WriteByte((byte)PacketType.auth_failure);
                NetworkManager.Instance.m_sendQueue.Enqueue(new PacketData(responsePacket, clientEndPoint));

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

        public void RemoveSession(IPEndPoint clientEndPoint)
        {
            if (m_authenticatedSessions.ContainsKey(clientEndPoint))
                m_authenticatedSessions.Remove(clientEndPoint);
        }

        private string GenerateSessionToken()
        {
            return $"{System.Guid.NewGuid()}";
        }

        private bool IsValidUser(string username)
        {
            //hekbas: implement friends only or blacklisting
            //return m_validUsers.Contains(username);
            return true;
        }
    }
}
