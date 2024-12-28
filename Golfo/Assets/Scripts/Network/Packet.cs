using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

namespace lv.network
{
    public enum PacketType : byte
    {
        connection_request,

        auth_success,
        auth_failure,
        invalid_session,
        expired_session,

        lobby_name,
        game_start,
        game_end,

        player_position,
        player_turn,
        obstacle1_data,

        chat_message,

        ball_strike,

        item_get,
        item_use,
        item_steal
    }

    public class Packet
    {
        public MemoryStream m_memoryStream;
        public BinaryReader m_reader;
        public BinaryWriter m_writer;

        public Packet()
        {
            m_memoryStream = new MemoryStream();
            m_reader = new BinaryReader(m_memoryStream);
            m_writer = new BinaryWriter(m_memoryStream);
        }

        public Packet(byte[] data)
        {
            m_memoryStream = new MemoryStream(data);
            m_reader = new BinaryReader(m_memoryStream);
            m_writer = new BinaryWriter(m_memoryStream);
        }

        public void SetStreamPos(long pos)
        {
            m_memoryStream.Position = pos;
        }

        public void WriteBool(bool value) => m_writer.Write(value);
        public void WriteByte(byte value) => m_writer.Write(value);
        public void WriteInt(int value) => m_writer.Write(value);
        public void WriteFloat(float value) => m_writer.Write(value);
        public void WriteVector3(Vector3 vec)
        {
            WriteFloat(vec.x);
            WriteFloat(vec.y);
            WriteFloat(vec.z);
        }
        public void WriteString(string text)
        {
            m_writer.Write(text.Length);
            m_writer.Write(Encoding.UTF8.GetBytes(text));
        }

        public bool ReadBool() => m_reader.ReadBoolean();
        public byte ReadByte() => m_reader.ReadByte();
        public int ReadInt() => m_reader.ReadInt32();
        public float ReadFloat() => m_reader.ReadSingle();
        public Vector3 ReadVector3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            return new Vector3(x, y, z);
        }
        public string ReadString()
        {
            int length = m_reader.ReadInt32();
            byte[] stringBytes = m_reader.ReadBytes(length);
            return Encoding.UTF8.GetString(stringBytes);
        }

        public byte[] GetData()
        {
            m_writer.Flush();
            return m_memoryStream.ToArray();
        }

        public void Close()
        {
            m_writer?.Close();
            m_reader?.Close();
            m_memoryStream?.Close();
        }
    }

    public class PacketData
    {
        public Packet m_packet { get; private set; }
        public IPEndPoint m_remoteEP { get; private set; }
        public bool m_isBroadCast { get; private set; }

        public PacketData(Packet packet, IPEndPoint remoteEP, bool isBroadCast = false)
        {
            m_packet = packet;
            m_remoteEP = remoteEP;
            m_isBroadCast= isBroadCast;
        }
    }
}
