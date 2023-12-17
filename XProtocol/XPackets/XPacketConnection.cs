using XProtocol.Serializer;

namespace XProtocol.XPackets;

[Serializable]
public class XPacketConnection
{
    [XField(1)] public bool IsSuccessful;
}