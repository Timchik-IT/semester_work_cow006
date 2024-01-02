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
    private int _maxPoint;

    private static Stack<byte> _cards = new();
    
    private int _activePlayerId;
    private string? _looserName;

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

    private void SendPackOfCards(List<byte> cardsId)
    {
        cardsId.Sort();
        foreach (var client in ConnectedClients)
            foreach (var cardId in cardsId)
            {
                client.SendDeckCard(cardId);
                Thread.Sleep(10);
            }
    }
    
    private void SendStarterPackOfCards(List<byte> cardForDeck)
    {
        foreach (var client in ConnectedClients)
            foreach (var cardId in cardForDeck)
                client.SendStarterDeckCard(cardId);
    }

    private void ResetDeckLists()
    {
        foreach (var clients in ConnectedClients)
            clients.ResetDecks();
    }

    public void StartGame()
    {
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
            InitializeGame();
            
            var cardForDeck = new List<byte>();
            for (var j = 0; j < 4; j++)
            {
                var card = _cards.Pop();
                cardForDeck.Add(card);
            }
            
            SendStarterPackOfCards(cardForDeck);
            Console.WriteLine("Send starter cards");
            
            foreach (var client in ConnectedClients)
            {
                for (var i = 0; i < 10; i++)
                    client.GiveCard(_cards.Pop());
                Console.WriteLine($"Cards for player {client.Name} are ready");
                client.Points = 0;
            }
            
            for (var i = 0; i < 10; i++)
            {
                var selectedCards = new List<byte>();
                
                for (var activePlayerId = 0; activePlayerId < 4; activePlayerId++)
                {
                    var activePlayer = ConnectedClients[activePlayerId];
                    Thread.Sleep(100);
                    
                    activePlayer.StartTurn();
                    
                    Console.WriteLine($"Player {activePlayer.Name} ({activePlayer.Id}) is moving now");
                    
                    while (true)
                    {
                        if (!activePlayer.Turn)
                            break;
                    }
                    Thread.Sleep(1000);
                    selectedCards.Add(activePlayer.SelectedCardId);
                    Console.WriteLine($"Player {activePlayer.Name} ({activePlayer.Id}) has finished his turn");
                }
                
                SendPackOfCards(selectedCards);
                Thread.Sleep(1000);
            }
            CheckEndOfGame();
        }
    }
    
    private void CheckEndOfGame()
    {
        var losersList = new List<byte>();
        
        foreach (var client in ConnectedClients.Where(client => client.Points > 66))
        {
            losersList.Add(client.Id);
            _isGameOver = true;
            Console.WriteLine("Game over");
        }

        if (losersList.Count == 0)
        {
            ResetDeckLists();
            Thread.Sleep(100);
            return;
        }
            
        
        foreach (var client in ConnectedClients.Where(client => losersList.Contains(client.Id)).Where(client => _maxPoint < client.Points))
        {
            _maxPoint = client.Points;
            _looserName = client.Name;
        }
        
        foreach (var client in ConnectedClients)
            client.EndOfGame(_looserName);
    }
}