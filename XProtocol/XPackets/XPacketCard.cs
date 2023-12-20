using XProtocol.Serializer;

namespace XProtocol.XPackets;

[Serializable]
public class XPacketCard
{
    [XField(1)] public byte CardId;

    public XPacketCard()
    {
    }

    public XPacketCard(byte cardId) => CardId = cardId;
}