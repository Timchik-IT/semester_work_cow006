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

    private static Stack<byte> _cards = new();
    private static Stack<byte> _list1 = new();
    private static Stack<byte> _list2 = new();
    private static Stack<byte> _list3 = new();
    private static Stack<byte> _list4 = new();

    
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
        _list1.Push(_cards.Pop());
        _list2.Push(_cards.Pop());
        _list3.Push(_cards.Pop());
        _list4.Push(_cards.Pop());
    }

    public void StartGame()
    {
        InitializeGame();

        while (true)
        {
            if (!ConnectedClients.All(x => x.IsReady)) continue;
            Thread.Sleep(100);
            break;
        }

        foreach (var client in ConnectedClients)
        {
            // Init players
            for (var i = 0; i < 10; i++)
                client.GiveCard(_cards.Pop());

            client.Points = 0;
        }
        
        _isGameOver = false;

        while (!_isGameOver)
        {
            for (var i = 0; i < 10; i++)
            {
                var activePlayer = ConnectedClients[_activePlayerId % 4];
                activePlayer.NextMove();

                Console.WriteLine($"{activePlayer.Name}'s turn");
                while (true)
                {
                    if (!activePlayer.Turn)
                        break;
                }

                Console.WriteLine($"Player {activePlayer.Name} has finished his turn");
                _activePlayerId += 1;
            }

            foreach (var client in ConnectedClients)
            {
                
            }
        }
    }
}