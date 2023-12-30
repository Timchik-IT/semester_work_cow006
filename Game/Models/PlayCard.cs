namespace Game.Models;

public class PlayCard
{
    public PlayCard(int id)
    {
        Id = (byte)id;
        Number = (byte)(id+1);
        Points = CalculatePoints(id);
    }

    private byte CalculatePoints(int id)
    {
        var numOfCard = id + 1;

        if (numOfCard % 5 != 0) return numOfCard % 11 == 0 ? (byte)5 : (byte)1;
        
        if (numOfCard % 10 == 0)
            return 3;
        
        return numOfCard % 11 == 0 ? (byte)7 : (byte)2;

    }
    
    public byte Number { get; }
    public byte Id { get; }
    public byte Points { get; }
}