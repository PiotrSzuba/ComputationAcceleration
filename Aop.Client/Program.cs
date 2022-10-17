using Aop.Client;
using Aop.RabbitMQ.Extensions;
using Aop.RabbitMQ.TSP;
using System.Diagnostics;

Console.WriteLine("Client started");

var tspClient = new TspClient();
tspClient.Run();

//RunNativeBruteforce();

Console.WriteLine("Press [enter] to exit.");
Console.ReadLine();

void RunNativeBruteforce()
{
    var tspFileReader = new TspFileReader("m13.atsp");
    var tspInput = new TspInput
    {
        Matrix = tspFileReader.ImMatrix,
    };

    var sw = new Stopwatch();
    sw.Start();
    var result = Bruteforce.Run(tspInput);
    sw.Stop();

    Console.WriteLine($"{result.Cost} opt: {tspFileReader.OptimalValue} {sw.ElapsedMilliseconds} ms");
}