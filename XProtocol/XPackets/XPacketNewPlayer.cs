using XProtocol.Serializer;

namespace XProtocol.XPackets;

[Serializable]
public class XPacketNewPlayer
{
    [XField(1)] public string? Name;

    [XField(2)] public uint Rgb;
}