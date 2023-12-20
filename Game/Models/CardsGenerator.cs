using System;
using System.Collections.Generic;

namespace Game.Models;

public static class CardsGenerator
{
    public static Dictionary<byte, PlayCard> GenerateListOfPlayCards()
    {
        var playCards = new Dictionary<byte, PlayCard>();
        
        for (byte id = 0; id < 104; id++)
        {
            playCards.Add(id, new PlayCard((byte)id+1));
        }

        return playCards;
    }
}
