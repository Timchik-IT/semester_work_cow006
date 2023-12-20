using XProtocol.Serializer;

namespace XProtocol.XPackets;

[Serializable]
public class XPacketPlayers
{
    [XField(1)] public List<(byte, string, string)>? Players;
    
    public XPacketPlayers()
    {
    }

    public XPacketPlayers(List<(byte, string, string)>? players) => Players = players;
}