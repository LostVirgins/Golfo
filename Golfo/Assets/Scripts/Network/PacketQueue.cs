using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace lv.network
{
    public class PacketQueue : MonoBehaviour
    {
        private Queue<PacketData> m_packetQueue;

        public void Enqueue(PacketData packet)
        {
            m_packetQueue.Enqueue(packet);
        }

        public bool Dequeue(out PacketData packet)
        {
            return m_packetQueue.TryDequeue(out packet);
        }

        public bool IsEmpty => m_packetQueue.Count == 0;

        public void Clear()
        {
            m_packetQueue.Clear();
        }
    }
}
