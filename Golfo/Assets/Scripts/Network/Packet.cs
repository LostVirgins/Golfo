using System;
using System.IO;
using System.Numerics;
using System.Text;

namespace lv.network
{
    public enum PacketType : byte
    {
        connection_request,

        auth_success,
        auth_failure,
        invalid_session,
        expired_session,

        player_movement,
        player_turn
    }

    public class Packet
    {
        private MemoryStream m_memoryStream;
        private BinaryWriter m_writer;
        private BinaryReader m_reader;

        public Packet()
        {
            m_memoryStream = new MemoryStream();
            m_writer = new BinaryWriter(m_memoryStream);
        }

        public Packet(byte[] data)
        {
            m_memoryStream = new MemoryStream(data);
            m_reader = new BinaryReader(m_memoryStream);
        }

        public void WriteByte(byte value) => m_writer.Write(value);
        public void WriteInt(int value) => m_writer.Write(value);
        public void WriteFloat(float value) => m_writer.Write(value);
        public void WriteVector3(Vector3 vec)
        {
            WriteFloat(vec.X);
            WriteFloat(vec.Y);
            WriteFloat(vec.Z);
        }
        public void WriteString(string text)
        {
            m_writer.Write(text.Length);
            m_writer.Write(Encoding.UTF8.GetBytes(text));
        }

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
}
