using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using XProtocol;
using XProtocol.Serializer;
using XProtocol.XPackets;

namespace TCPServer;

public class ConnectedClient
{
    public readonly byte Id;
    
    private Socket Client { get; }

    private readonly Queue<byte[]> _packetSendingQueue = new();

    private static readonly List<string> ColorsList = new()
    {
        "Red", "Yellow", "Green", "Blue"
    };

    private readonly Random _random = new();
    
    private string? _name;
    private bool _turn;
    private string? _color;
    private byte _points;
    private byte _cardsCount;

    public bool IsReady { get; private set; }
    
    public List<byte>? Cards { get; private init; }
    
    public byte CardsCount
    {
        get => _cardsCount;
        set
        {
            _cardsCount = value;
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
    
    public string? Name
    {
        get => _name;
        private set
        {
            _name = value;
            var colorsCount = ColorsList.Count;
            var randomNum = _random.Next(colorsCount);
            Color = ColorsList[randomNum];
            QueuePacketSend(XPacketConverter.Serialize(XPacketType.UpdatedPlayerProperty,
                new XPacketUpdatedPlayerProperty(Id, nameof(Color),
                    Color.GetType(), Color)).ToPacket());
            ColorsList.RemoveAt(randomNum);
        }
    }
    
    public string? Color
    {
        get => _color;
        set
        {
            _color = value;
            SendPlayers();
            Console.WriteLine($"Connected player with name: {Name}" +
                              $"\nGiven color: {_color}");
            IsReady = true;
        }
    }
    
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    public event PropertyChangedEventHandler? PropertyChanged;
    

    public ConnectedClient(Socket client, byte id)
    {
        Client = client;
        Id = id;
        Cards = new List<byte>();

        Task.Run(ReceivePackets);
        Task.Run(SendPackets);
    }

    private void ReceivePackets()
    {
        while (true)
        {
            var buff = new byte[512];
            Client.Receive(buff);

            var decryptedBuff = XProtocolEncryptor.Decrypt(buff);

            buff = decryptedBuff.TakeWhile((b, i) =>
            {
                if (b != 0xFF) return true;
                return decryptedBuff[i + 1] != 0;
            }).Concat(new byte[] { 0xFF, 0 }).ToArray();

            var parsed = XPacket.Parse(buff);

            if (parsed != null!) ProcessIncomingPacket(parsed);
        }
    }
    
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

            Client.Send(encryptedPacket);

            Thread.Sleep(100);
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
            case XPacketType.NewMove:
                ProcessEndTurn();
                break;
            case XPacketType.Unknown:
                break;
            case XPacketType.PlayersList:
                break;
            case XPacketType.Points:
                ProcessSettingPoints(packet);
                break;
            default:
                throw new ArgumentException("Получен неизвестный пакет");
        }
    }
    
    internal void Update(byte id, string? objectName, object? obj)
        => QueuePacketSend(XPacketConverter.Serialize(XPacketType.UpdatedPlayerProperty,
                new XPacketUpdatedPlayerProperty(id, objectName, obj!.GetType(), obj))
            .ToPacket());
    
    private void QueuePacketSend(byte[] packet)
        => _packetSendingQueue.Enqueue(packet);
    
    private void ProcessConnection(XPacket packet)
    {
        var packetConnection = XPacketConverter.Deserialize<XPacketConnection>(packet);
        packetConnection.IsSuccessful = true;

        QueuePacketSend(XPacketConverter.Serialize(XPacketType.Connection, packetConnection).ToPacket());

        QueuePacketSend(XPacketConverter.Serialize(XPacketType.UpdatedPlayerProperty,
            new XPacketUpdatedPlayerProperty(Id, nameof(Id), Id.GetType(), Id)).ToPacket());
    }
    
    private void ProcessUpdatingProperty(XPacket packet)
    {
        var packetProperty = XPacketConverter.Deserialize<XPacketUpdatedPlayerProperty>(packet);
        switch (packetProperty.PropertyName)
        {
            case "Name":
            {
                Name = Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!) as string;
                break;
            }
            default:
            {
                var property = typeof(ConnectedClient).GetProperty(packetProperty.PropertyName!);
                property!.SetValue(this,
                    Convert.ChangeType(packetProperty.PropertyValue, packetProperty.PropertyType!));
                OnPropertyChanged(property.Name);
                break;
            }
        }
    }
    
    private static void SendPlayers()
    {
        var players = XServer.ConnectedClients.Select(x => x.GetPlayerParameters()).ToList();
        var packet = XPacketConverter.Serialize(XPacketType.PlayersList,
            new XPacketPlayers { Players = players });
        var bytePacket = packet.ToPacket();
        foreach (var client in XServer.ConnectedClients)
            client.QueuePacketSend(bytePacket);
    }
    
    private (byte, string, string) GetPlayerParameters() => (Id, Name, Color!)!;

    private void ProcessEndTurn() => Turn = false;

    private void ProcessSettingPoints(XPacket packet)
    {
        var xPacketPoints = XPacketConverter.Deserialize<XPacketPoints>(packet);
        Points = xPacketPoints.Points;
    }
    
    public void GiveCard(byte cardId)
    {
        Cards!.Add(cardId);
        CardsCount++;
        var packetCard = XPacketConverter.Serialize(XPacketType.Card, new XPacketCard(cardId)).ToPacket();
        QueuePacketSend(packetCard);
    }
    
    public void NextMove()
    {
        Turn = true;
        var packetTurn = XPacketConverter.Serialize(XPacketType.NewMove, new XPacketNewMove()).ToPacket();
        QueuePacketSend(packetTurn);
    }

    public void UpdatePoints()
    {
        var packetPoints = XPacketConverter.Serialize(XPacketType.Points, Points).ToPacket();
        QueuePacketSend(packetPoints);
    }
}
