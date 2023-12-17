using System.Net;
using System.Net.Sockets;

namespace TCPServer;

internal class XServer
{
    private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    internal static readonly List<ConnectedClient> Clients = new();

    private bool _listening;
    private bool _stopListening;
    private bool _isGameOver;

    private static Stack<byte> _cards = new();
    private readonly Random _random = new();
    
    private int _activePlayerId;

    public Task StartAsync()
    {
        try
        {
            if (_listening)
                throw new Exception("Server is already listening requests..");
            
            _socket.Bind(new IPEndPoint(IPAddress.Any, 9218));
            _socket.Listen(10);

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

            if (Clients.Count < 4)
            {
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

                var c = new ConnectedClient(client, (byte)Clients.Count);

                Clients.Add(c);
            }


            if (Clients.All(x => x.IsReady) && Clients.Count == 4)
                break;
        }
    }
    
    private void InitializeGame()
    {
        var cards = new List<byte>();
        for (byte id = 0; id < 104; id++) 
            cards.Add(id);
        cards = cards.OrderBy(x => _random.Next()).ToList();
        foreach (var card in cards)
            _cards.Push(card);
    }

    public Task StartGameAsync()
    {
        InitializeGame();

        foreach (var client in Clients)
        {
            //TODO Init Players 
        }

        _isGameOver = false;
        
        while (!_isGameOver)
        {
            var activePlayer = Clients[_activePlayerId % 4];
            activePlayer.SendNextMove();
            while (true)
            {
                if (!activePlayer.Turn)
                    break;
            }

            Console.WriteLine($"Player {activePlayer.GetName()} has finished his turn");
            _activePlayerId += 1;
        }

        return Task.CompletedTask;
    }
}