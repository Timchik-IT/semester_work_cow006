namespace XProtocol;

public enum XPacketType
{
    Unknown,
    Connection,
    PlayersList,
    UpdatedPlayerProperty,
    ResetDeck,
    Card,
    DeckCard,
    CreateDeckListsCard,
    Points,
    Loser,
    NewMove
}