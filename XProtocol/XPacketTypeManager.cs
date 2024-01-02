namespace XProtocol;

public static class XPacketTypeManager
{
    private static readonly Dictionary<XPacketType, Tuple<byte, byte>> TypeDictionary = new();

    static XPacketTypeManager()
    {
        RegisterType(XPacketType.Connection, 1, 0);
        RegisterType(XPacketType.UpdatedPlayerProperty, 2, 0);
        RegisterType(XPacketType.PlayersList, 3, 0);
        RegisterType(XPacketType.NewMove, 4, 0);
        RegisterType(XPacketType.Card, 5, 0);
        RegisterType(XPacketType.DeckCard, 5, 1);
        RegisterType(XPacketType.CreateDeckListsCard, 5, 2);
        RegisterType(XPacketType.ResetDeck, 5, 3);
        RegisterType(XPacketType.Loser, 6, 0);
        RegisterType(XPacketType.Points, 7, 0);
    }

    private static void RegisterType(XPacketType type, byte btype, byte bsubtype)
    {
        if (TypeDictionary.ContainsKey(type))
            throw new Exception($"Packet type {type:G} is already registered.");

        TypeDictionary.Add(type, Tuple.Create(btype, bsubtype));
    }

    public static Tuple<byte, byte> GetType(XPacketType type)
    {
        if (!TypeDictionary.TryGetValue(type, out var value))
            throw new Exception($"Packet type {type:G} is not registered.");

        return value;
    }

    public static XPacketType GetTypeFromPacket(XPacket packet)
    {
        var type = packet.PacketType;
        var subtype = packet.PacketSubtype;

        foreach (var (xPacketType, tuple) in TypeDictionary)
        {
            if (tuple.Item1 == type && tuple.Item2 == subtype)
                return xPacketType;
        }

        return XPacketType.Unknown;
    }
}