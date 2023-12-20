using XProtocol.Serializer;

namespace XProtocol.XPackets;

[Serializable]
public class XPacketUpdatedPlayerProperty
{
    [XField(1)] public byte PlayerId;

    [XField(2)] public string? PropertyName;

    [XField(3)] public Type? PropertyType;

    [XField(4)] public object? PropertyValue;

    public XPacketUpdatedPlayerProperty()
    {
    }

    public XPacketUpdatedPlayerProperty(byte playerId, string? propertyName, Type? propertyType, object? propertyValue)
    {
        PlayerId = playerId;
        PropertyName = propertyName;
        PropertyType = propertyType;
        PropertyValue = propertyValue;
    }
}