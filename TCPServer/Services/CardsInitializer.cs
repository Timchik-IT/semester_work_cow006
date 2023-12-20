namespace TCPServer.Services;

public class CardsInitializer
{
    private readonly Random _random = new();

    internal Stack<byte> GetCards()
    {
        var playCards = new List<byte>();
        for (byte id = 0; id < 104; id++)
            playCards.Add(id);

        var cardsDeck = new Stack<byte>();
        playCards = playCards.OrderBy(x => _random.Next()).ToList();
        foreach (var card in playCards)
            cardsDeck.Push(card);

        return cardsDeck;
    }
}