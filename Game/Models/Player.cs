﻿using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using XProtocol;
using XProtocol.Serializer;
using XProtocol.XPackets;

namespace Game.Models;

public sealed class Player : INotifyPropertyChanged
{
    private readonly Dictionary<byte, PlayCard> _playCards = new();
    
    private readonly Queue<byte[]> _packetSendingQueue = new();
    private ObservableCollection<Player>? _playersList = null!;
    
    private readonly List<PlayCard> _playerCards = null!;
    
    private byte _id; 
    private string _name = null!;
    private string _color = null!;
    private bool _playerReady;
    private byte _points;
    private bool _turn;
    private PlayCard _selectedCard;
    
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
    
    public bool PlayerReady
    {
        get => _playerReady;
        set
        {
            _playerReady = value;
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
    
    public List<PlayCard> PlayerCards
    {
        get => _playerCards;
        init
        {
            _playerCards = value;
            OnPropertyChanged();
        }
    }

    public PlayCard SelectedCard
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
        PlayerCards = new List<PlayCard>();
    }
    
    private Player(byte id) => Id = id;
    
    public Player()
    {
        PlayersList = new ObservableCollection<Player> { new(0), new(1), new(2), new(3) };
        Name = "";
        PlayerCards = new List<PlayCard>();

        _playCards = CardsGenerator.GenerateListOfPlayCards();;
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
            var buff = new byte[512];
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
                break;
            case XPacketType.UpdatedPlayerProperty:
                ProcessUpdatingProperty(packet);
                break;
            case XPacketType.PlayersList:
                ProcessGettingPlayers(packet);
                break;
            case XPacketType.Card:
                ProcessGettingCard(packet);
                break;
            case XPacketType.NewMove:
                ProcessStartingTurn(packet);
                break;
            case XPacketType.Unknown:
                break;
            default:
                throw new ArgumentException("Получен неизвестный пакет");
        }
    }

    private void ProcessStartingTurn(XPacket packet) 
        => Turn = true;
    
    private void ProcessGettingCard(XPacket packet)
    {
        var packetCard = XPacketConverter.Deserialize<XPacketCard>(packet);
        PlayerCards.Add(_playCards[packetCard.CardId]);
        OnPropertyChanged(nameof(PlayerCards));
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
            Console.WriteLine("Handshake successful!");
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
            case "SelectedCardId":
            {
                var value = (byte)Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!)!;
                SelectedCard = _playCards[value];
                break;
            }
            default:
            {
                var property = GetType().GetProperty(packetProperty.PropertyName!);
                property!.SetValue(PlayersList![packetProperty.PlayerId],
                    Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!));
                OnPropertyChanged(nameof(PlayersList));
                if (packetProperty.PlayerId == Id)
                {
                    property.SetValue(this,
                        Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!)!);
                    OnPropertyChanged(property.Name);
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

    internal void EndTurn()
    {
        Turn = false;
        var packet = XPacketConverter.Serialize(XPacketType.NewMove, new XPacketNewMove()).ToPacket();
        QueuePacketSend(packet);
    }
}