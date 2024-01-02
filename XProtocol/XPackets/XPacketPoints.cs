using XProtocol.Serializer;

namespace XProtocol.XPackets;

[Serializable]
public class XPacketPoints
{
    [XField(1)] public int Points;

    public XPacketPoints()
    {
        
    }

    public XPacketPoints(int points)
    {
        Points = points;   
    }
}