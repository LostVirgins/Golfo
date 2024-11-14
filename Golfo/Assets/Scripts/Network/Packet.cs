using System.IO;
using System.Text;

public class Packet
{
    private MemoryStream memoryStream;
    private BinaryWriter writer;
    private BinaryReader reader;

    public Packet()
    {
        memoryStream = new MemoryStream();
        writer = new BinaryWriter(memoryStream);
    }

    public Packet(byte[] data)
    {
        memoryStream = new MemoryStream(data);
        reader = new BinaryReader(memoryStream);
    }

    public void WriteInt(int value) => writer.Write(value);
    public void WriteString(string value) => writer.Write(Encoding.UTF8.GetBytes(value));

    public int ReadInt() => reader.ReadInt32();
    public string ReadString() => Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));

    public byte[] GetData()
    {
        writer.Flush();
        return memoryStream.ToArray();
    }

    public void Close()
    {
        writer?.Close();
        reader?.Close();
        memoryStream?.Close();
    }
}
