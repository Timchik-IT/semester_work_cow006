namespace Game.Models;

public class PlayCard
{
    public PlayCard(int id)
    {
        Id = (byte)id;
        Points = CalculatePoints(id);
    }

    private byte CalculatePoints(int id)
    {
        var numOfCard = id + 1;

        if (numOfCard % 5 == 0)
        {
            if (numOfCard % 10 == 0)
                return 3;
            return numOfCard % 11 == 0 ? (byte)7 : (byte)2;
        }

        return numOfCard % 11 == 0 ? (byte)5 : (byte)1;
    }

    public byte Id { get; }
    public byte Points { get; }
}