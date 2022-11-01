using Aop.Client;
using Aop.RabbitMQ.TSP;
using System.Diagnostics;

Console.WriteLine("Client started");

var tspClient = new TspClient();
tspClient.Run();

//runGenetic();

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();

void runGenetic()
{
    var tspFileReader = new TspFileReader("gr202.tsp");
    var tspInput = new TspInput
    {
        Matrix = tspFileReader.ImMatrix,
    };
    Stopwatch sw = new();
    sw.Start();
    var genetic = new Genetic(tspInput);
    var output = genetic.Run();
    sw.Stop();
    Console.WriteLine($"{output.Cost} time: {sw.ElapsedMilliseconds}ms");
}