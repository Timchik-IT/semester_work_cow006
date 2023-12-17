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

    public Task StartGame()
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
    
    
    
    
    
    
    
    
}