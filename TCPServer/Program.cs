using TCPServer;

Console.Title = "XServer";

var server = new XServer();
await server.StartAsync();
server.AcceptClients();
server.StartGame();
