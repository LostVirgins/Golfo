using System.IO;
using System.Text;

namespace lv.network
{
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

        public void WriteInt(int value) => m_writer.Write(value);
        public void WriteString(string value) => m_writer.Write(Encoding.UTF8.GetBytes(value));

        public int ReadInt() => m_reader.ReadInt32();
        public string ReadString() => Encoding.UTF8.GetString(m_reader.ReadBytes(m_reader.ReadInt32()));

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
