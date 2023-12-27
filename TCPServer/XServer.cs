using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using TCPServer.Services;
using XProtocol;
using XProtocol.Serializer;
using XProtocol.XPackets;

namespace TCPServer;

internal class XServer
{
    private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    internal static readonly List<ConnectedClient> ConnectedClients = new();

    private bool _listening;
    private bool _stopListening;
    private bool _isGameOver;
    private byte _maxPoint;

    private static Stack<byte> _cards = new();
    
    private int _activePlayerId;

    public Task StartAsync()
    {
        try
        {
            if (_listening)
                throw new Exception("Server is already listening incoming requests.");

            _socket.Bind(new IPEndPoint(IPAddress.Any, 1410));
            _socket.Listen(4);

            _listening = true;

            Console.WriteLine("Server has been started");
            var stopThread = new Thread(() =>
            {
                while (_listening)
                    if (Console.ReadLine() == "stop")
                        Stop();
            });
            stopThread.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return Task.CompletedTask;
    }

    private void Stop()
    {
        if (!_listening)
            throw new Exception("Server close listening");
        _stopListening = true;
        _listening = false;
        _socket.Close();
        Console.WriteLine("Server has been closed.");
    }
    
    public void AcceptClients()
    {
        while (true)
        {
            if (_stopListening)
                return;
            Socket client;

            try
            {
                client = _socket.Accept();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(10000);
                continue;
            }

            Console.WriteLine($"[!] Accepted client from {(IPEndPoint)client.RemoteEndPoint!}");

            var c = new ConnectedClient(client, (byte)ConnectedClients.Count);

            ConnectedClients.Add(c);
            
            c.PropertyChanged += Client_PropertyChanged!;

            if (ConnectedClients.Count == 4)
                break;
        }
    }
    
    private static void Client_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var client = sender as ConnectedClient;
        foreach (var connectedClient in ConnectedClients)
        {
            var id = client!.Id;
            var propertyName = e.PropertyName;
            var type = client.GetType();
            var property = type.GetProperty(e.PropertyName!);
            var value = property!.GetValue(client);
            connectedClient.Update(id, propertyName, value);
        }
    }
    
    private void InitializeGame()
    {
        _cards = new CardsInitializer().GetCards();
        Console.WriteLine("Cards ready!");
    }

    private void SendPackOfCards(byte[] cardsId)
    {
        foreach (var client in ConnectedClients)
            foreach (var cardId in cardsId)
                client.SendDeckCard(cardId);
    }

    public void StartGame()
    {
        InitializeGame();

        while (true)
        {
            if (!ConnectedClients.All(x => x.IsReady))
            {
                Thread.Sleep(1000);
                continue;
            }
            break;
        }
        
        _isGameOver = false;
        Console.WriteLine("Game is start");
        
        while (!_isGameOver)
        {
            var startedCards = new[]
            {
                _cards.Pop(),
                _cards.Pop(),
                _cards.Pop(),
                _cards.Pop()
            };
            SendPackOfCards(startedCards);
            Console.WriteLine("Cards for play deck are ready");
            
            
            foreach (var client in ConnectedClients)
            {
                for (var i = 0; i < 10; i++)
                    client.GiveCard(_cards.Pop());
                Console.WriteLine($"Cards for player {client.Name} are ready");
                client.Points = 0;
            }
            
            for (var i = 0; i < 10; i++)
            {
                for (var activePlayerId = 0; activePlayerId < 4; activePlayerId++)
                {
                    var activePlayer = ConnectedClients[activePlayerId];
                    activePlayer.StartTurn();
                    Console.WriteLine($"player {activePlayer.Name} ({activePlayer.Id}) is moving now");

                    while (true)
                    {
                        if (!activePlayer.Turn)
                            break;
                    }

                    Console.WriteLine($"Player {activePlayer.Name} ({activePlayer.Id}) has finished his turn");
                }

                var selectedCards = ConnectedClients.Select(x => x.SelectedCardId).ToList();
                selectedCards.Sort();
                SendPackOfCards(selectedCards.ToArray());
            }

            foreach (var client in ConnectedClients)
            {
                if (client.Points < 66) continue;
                _isGameOver = true;
                Console.WriteLine("Game over");
            }
        }
    }
}