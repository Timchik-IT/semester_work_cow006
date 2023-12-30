using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Game.Models;

public static class CardsManager
{
    public static Dictionary<byte, PlayCard> GenerateListOfPlayCards()
    {
        var playCards = new Dictionary<byte, PlayCard>();
        
        for (byte id = 0; id < 104; id++)
        {
            playCards.Add(id, new PlayCard(id));
        }

        return playCards;
    }
}
