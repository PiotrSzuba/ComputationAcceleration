using Aop.Client;

Console.WriteLine("Client started");

var tspClient = new TspClient();
tspClient.Run();

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();