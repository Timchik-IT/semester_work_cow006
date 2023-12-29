﻿using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using XProtocol;
using XProtocol.Serializer;
using XProtocol.XPackets;

namespace Game.Models;

public sealed class Player : INotifyPropertyChanged
{
    private bool _gameIsOver = false;
    private string _loserName = null!;
    
    private readonly Dictionary<byte, PlayCard> _playCards = CardsManager.GenerateListOfPlayCards();
    private readonly Queue<byte[]> _packetSendingQueue = new();
    
    private ObservableCollection<Player>? _playersList = null!;
    private List<ObservableCollection<PlayCard>> _deckLists = null!;
    private readonly ObservableCollection<PlayCard> _playerCards = null!;
    
    private byte _id; 
    private string _name = null!;
    private string _color = null!;
    private byte _points;
    private bool _turn;
    private PlayCard _selectedCard = null!;
    private bool _isReady;

    public string LoserName
    {
        get => _loserName;
        set
        {
            _loserName = value;
            OnPropertyChanged();
        }
    }

    public bool GameIsOver
    {
        get => _gameIsOver;
        set
        {
            _gameIsOver = value;
            OnPropertyChanged();
        }
    }
    
    public List<ObservableCollection<PlayCard>> DeckLists
    {
        get => _deckLists;
        set
        {
            _deckLists = value;
            OnPropertyChanged();
        }
    }
    
    public byte Id
    {
        get => _id;
        set
        {
            _id = value;
            OnPropertyChanged();
        }
    }


    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }


    public string? Color
    {
        get => _color;
        set
        {
            _color = value;
            OnPropertyChanged();
        }
    }
    
    public byte Points
    {
        get => _points;
        set
        {
            _points = Points;
            OnPropertyChanged();
        }
    }
    
    public bool Turn
    {
        get => _turn;
        set
        {
            _turn = value;
            OnPropertyChanged();
        }
    }

    public bool IsReady
    {
        get => _isReady;
        set
        {
            _isReady = value;
            OnPropertyChanged();
        }
    }
    
    public ObservableCollection<PlayCard> PlayerCards
    {
        get => _playerCards;
        init
        {
            _playerCards = value;
            OnPropertyChanged();
        }
    }

    private PlayCard SelectedCard
    {
        get => _selectedCard;
        set
        {
            _selectedCard = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Player>? PlayersList
    {
        get => _playersList;
        set
        {
            if (value == null) return;
            _playersList = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private Player(byte id, string name, string color)
    {
        Id = id;
        Name = name;
        Color = color;
        PlayerCards = new ObservableCollection<PlayCard>();
    }
    
    private Player(byte id) => Id = id;
    
    public Player()
    {
        PlayersList = new ObservableCollection<Player> { new(0), new(1), new(2), new(3) };
        Name = "";
        PlayerCards = new ObservableCollection<PlayCard>();
        DeckLists = new List<ObservableCollection<PlayCard>>();
        for (var i = 0; i < 4; i++)
            DeckLists.Add(new ObservableCollection<PlayCard>());
    }
    
    private Socket? _socket;
    private IPEndPoint? _serverEndPoint;

    internal void Connect()
    {
        try
        {
            Connect("127.0.0.1", 1410);

            QueuePacketSend(XPacketConverter.Serialize(XPacketType.Connection,
                new XPacketConnection
                {
                    IsSuccessful = false
                }).ToPacket());

            Thread.Sleep(300);

            QueuePacketSend(XPacketConverter.Serialize(XPacketType.UpdatedPlayerProperty,
                    new XPacketUpdatedPlayerProperty(Id, nameof(Name), Name.GetType(), Name))
                .ToPacket());

            while (true)
            {
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void Connect(string ip, int port) => Connect(new IPEndPoint(IPAddress.Parse(ip), port));

    private void Connect(IPEndPoint? server)
    {
        _serverEndPoint = server;

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(_serverEndPoint!);

        Task.Run(ReceivePackets);
        Task.Run(SendPackets);
    }

    private void ReceivePackets()
    {
        while (true)
        {
            var buff = new byte[1024];
            _socket!.Receive(buff);

            var decryptedBuff = XProtocolEncryptor.Decrypt(buff);

            var packetBuff = decryptedBuff.TakeWhile((b, i) =>
            {
                if (b != 0xFF) return true;
                return decryptedBuff[i + 1] != 0;
            }).Concat(new byte[] { 0xFF, 0 }).ToArray();
            var parsed = XPacket.Parse(packetBuff);

            if (parsed != null!) ProcessIncomingPacket(parsed);
        }
    }

    private void ProcessIncomingPacket(XPacket packet)
    {
        var type = XPacketTypeManager.GetTypeFromPacket(packet);

        switch (type)
        {
            case XPacketType.Connection:
                ProcessConnection(packet);
                Console.WriteLine("Пакет подключения");
                break;
            case XPacketType.UpdatedPlayerProperty:
                ProcessUpdatingProperty(packet);
                Console.WriteLine("Пакет обновления");
                break;
            case XPacketType.PlayersList:
                ProcessGettingPlayers(packet);
                Console.WriteLine("Пакет клиентов");
                break;
            case XPacketType.Card:
                ProcessGettingCard(packet);
                Console.WriteLine("Пакет карты");
                break;
            case XPacketType.DeckCard:
                ProcessUpdatingDeckLists(packet);
                Console.WriteLine("Пакет карты для стола");
                break;
            case XPacketType.NewMove:
                ProcessStartingTurn(packet);
                Console.WriteLine("Пакет хода");
                break;
            case XPacketType.Loser:
                ProcessEndingGame(packet);
                Console.WriteLine("Конец игры");
                break;
            case XPacketType.Unknown:
                break;
            default:
                throw new ArgumentException("Получен неизвестный пакет");
        }
    }

    private void ProcessEndingGame(XPacket packet)
    {
        var loserPacket = XPacketConverter.Deserialize<XPacketLoser>(packet);
        LoserName = loserPacket.LoserName;
        GameIsOver = true;
    }

    private void ProcessStartingTurn(XPacket packet) 
        => Turn = true;
    
    private void ProcessUpdatingDeckLists(XPacket packet)
    {
        var packetCard = XPacketConverter.Deserialize<XPacketCard>(packet);
        UpdateDeckLists(packetCard.CardId);
        Console.WriteLine($"Обработана карта для стола - {packetCard.CardId}");
    }
    
    private void ProcessGettingCard(XPacket packet)
    {
        var packetCard = XPacketConverter.Deserialize<XPacketCard>(packet);
        PlayerCards.Add(_playCards[packetCard.CardId]);
    }

    private void ProcessGettingPlayers(XPacket packet)
    {
        var packetPlayer = XPacketConverter.Deserialize<XPacketPlayers>(packet);
        var playersFromPacket = packetPlayer.Players;
        var playersList = playersFromPacket!.Select(x => new Player(x.Item1, x.Item2, x.Item3)).ToList();
        foreach (var player in playersList)
        {
            PlayersList![player.Id] = playersList[player.Id];
            OnPropertyChanged(nameof(PlayersList));
        }
            
        playersList[Id] = this;
        OnPropertyChanged(nameof(PlayersList));
    }

    private static void ProcessConnection(XPacket packet)
    {
        var connection = XPacketConverter.Deserialize<XPacketConnection>(packet);

        if (connection.IsSuccessful)
        {
            Console.WriteLine("Handshake successful!");
        }
    }

    private void ProcessUpdatingProperty(XPacket packet)
    {
        var packetProperty = XPacketConverter.Deserialize<XPacketUpdatedPlayerProperty>(packet);

        switch (packetProperty.PropertyName!)
        {
            case "Id":
            {
                Id = (byte)Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!)!;
                break;
            }
            case "ColorString":
            {
                Color = (packetProperty.PropertyValue as string)!;
                break;
            }
            case "SelectedCard":
            {
                break;
            }
            case "PlayerCards":
            {
                break;
            }
            default:
            {
                var property = GetType().GetProperty(packetProperty.PropertyName!);
                if (property != null)
                {
                    property!.SetValue(PlayersList![packetProperty.PlayerId],
                        Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!));
                    OnPropertyChanged(nameof(PlayersList));
                    if (packetProperty.PlayerId == Id)
                    {
                        property.SetValue(this,
                            Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!)!);
                        OnPropertyChanged(property.Name);
                    }
                }

                break;
            }
        }
    }

    private void QueuePacketSend(byte[] packet)
        => _packetSendingQueue.Enqueue(packet);

    private void SendPackets()
    {
        while (true)
        {
            if (_packetSendingQueue.Count == 0)
                continue;

            var packet = _packetSendingQueue.Dequeue();
            var encryptedPacket = XProtocolEncryptor.Encrypt(packet);

            if (encryptedPacket.Length > 512)
                throw new Exception("Max packet size is 512 bytes.");

            _socket!.Send(encryptedPacket);

            Thread.Sleep(100);
        }
    }
    
    private void UpdateDeckLists(byte cardId)
    {
        var card = _playCards[cardId];
        foreach (var list in DeckLists.Where(list => list.Count == 0))
        {
            list.Add(card);
            return;
        }
        
        var count = 0;
        var listI = new List<int>();
        for(var i = 0; i < 4; i++)
        {
            if (DeckLists[i].Last().Number < card.Number)
                count++;
            else
                listI.Add(i);
        }
        
        if (count == 4)
        {
            var listId = 0;
            var minPoints = 999;
            for (var index = 0; index < DeckLists.Count; index++)
            {
                var list = DeckLists[index];
                var currentPoints = list.Aggregate(0, (current, currentCard) => current + currentCard.Points);
                if (currentPoints >= minPoints) continue;
                
                minPoints = currentPoints;
                listId = index;
            }
            DeckLists[listId] = new ObservableCollection<PlayCard> { card };
            
            if (!PlayerCards.Contains(card)) return;
            PlayerCards.Remove(card);
            Points += (byte)minPoints;
        }
        else
        {
            var temp = 105;
            var listId = 0;
            var updatedList = new ObservableCollection<PlayCard>();
            foreach (var i in listI)
            {
                var diff = card.Number - DeckLists[i].Last().Number;
                if (diff >= temp && diff > 0) continue;
                temp = diff;
                listId = i;
                updatedList = DeckLists[listId];
            }
            
            if (updatedList.Count == 5)
            {
                if (SelectedCard.Id == cardId)
                {
                    foreach (var listCard in updatedList)
                    {
                        Points += listCard.Points;
                    }
                }

                updatedList = new ObservableCollection<PlayCard>();
            }
        
            updatedList.Add(card);
            _deckLists[listId] = updatedList;   
        }
    }
    
    internal void SelectCard(byte idCard)
    {
        SelectedCard = _playCards[idCard];
        Console.WriteLine($"Selected card {SelectedCard.Number}");
        IsReady = true;
    }
    
    internal void EndTurn()
    {
        PlayerCards.Remove(SelectedCard);
        Turn = false;
        IsReady = false;
        var movePacket = XPacketConverter.Serialize(XPacketType.NewMove, new XPacketNewMove()).ToPacket();
        var cardPacket = XPacketConverter.Serialize(XPacketType.DeckCard, new XPacketCard(SelectedCard.Id)).ToPacket();
        QueuePacketSend(cardPacket);
        QueuePacketSend(movePacket);
    }
}