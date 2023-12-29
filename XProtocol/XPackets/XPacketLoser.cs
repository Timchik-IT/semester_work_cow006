using XProtocol.Serializer;

namespace XProtocol.XPackets;

[Serializable]
public class XPacketLoser
{
    [XField(1)] public string LoserName;

    public XPacketLoser()
    {
        
    }

    public XPacketLoser(string loserName) => LoserName = loserName;
}