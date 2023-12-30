using XProtocol.Serializer;

namespace XProtocol.XPackets;

[Serializable]
public class XPacketPoints
{
    [XField(1)] public byte Points;

    public XPacketPoints()
    {
        
    }

    public XPacketPoints(byte points)
    {
        Points = points;   
    }
}