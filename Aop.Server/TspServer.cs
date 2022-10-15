using Aop.RabbitMQ;
using Aop.RabbitMQ.TSP;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text.Json;
using RabbitMQ.Client.Events;
using System.Collections.Immutable;

namespace Aop.Server;

public class TspServer
{
    private Sender<TspInput> Sender { get; set; }
    private Receiver<TspOutput> Receiver { get; set; }
    private Stopwatch Stopwatch { get; set; }
    private ImmutableArray<ImmutableArray<int>> Matrix { get; set; }
    private int ConsumersCount { get; set; } = 0;
    private int Cost { get; set; }
    private List<int> Path { get; set; }
    private int OptimalValue { get; set; }
    private int ReceivedMessagesCount = 0;
    private int MaxMessagesCount = 0;

    public bool IsRunning => Receiver.Channel.IsOpen && Receiver.Consumer.IsRunning;

    public TspServer()
    {
        Sender = new Sender<TspInput>();
        Receiver = new Receiver<TspOutput>();
        Stopwatch = new();
        Path = new();
    }

    public void Run(TspAlgoritms tspAlgoritm, TspFileReader tspFileReader)
    {
        Stopwatch = new();
        Stopwatch.Start();
        Cost = int.MaxValue;
        Path = new();
        OptimalValue = tspFileReader.OptimalValue;
        ConsumersCount = Sender.GetReceiversCount();

        if (!IsRunning)
        {
            Receiver.ReconnectToChannel();
        }

        Receiver.ClearQueue();

        Console.WriteLine($" [*] Server started amount of hungry consumers: {Sender.GetReceiversCount()}");

        Receiver.Consumer.Received += OnReceive;

        Receiver.Channel.BasicConsume(queue: Receiver.QueueName,autoAck: false,consumer: Receiver.Consumer);

        ReceivedMessagesCount = 0;

        PrepereQueue(tspAlgoritm, tspFileReader);

        Console.WriteLine("queue full");
    }

    private void OnReceive(object? sender, BasicDeliverEventArgs eventArgs)
    {
        var tspOutput = Receiver.DeserializeInput(eventArgs);

        if (tspOutput.Cost < Cost)
        {
            Cost = tspOutput.Cost;
            Path = tspOutput.BestPath;
            Console.WriteLine($" [x] Received cost: {tspOutput.Cost}, Received so far: {ReceivedMessagesCount + 1}");
        }

        Receiver.Channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
        ReceivedMessagesCount++;

        if (ReceivedMessagesCount == MaxMessagesCount)
        {
            Receiver.ClearQueue();
            Receiver.Channel.Close();
            Stopwatch.Stop();
            int proccesTime = Convert.ToInt32(Stopwatch.ElapsedMilliseconds);

            Console.WriteLine($"Task completed in {proccesTime} ms");
            Console.WriteLine($"Calculated cost: {Cost}");
            Console.WriteLine($"Target value was {OptimalValue}");
        }
    }

    private void PrepereQueue(TspAlgoritms tspAlgoritm, TspFileReader tspFileReader)
    {
        switch (tspAlgoritm)
        {
            case TspAlgoritms.Bruteforce:
                PrepareQueueForBruteForce(tspFileReader);
                break;
        }
    }
    
    private void PrepareQueueForBruteForce(TspFileReader tspFileReader)
    {
        var sw = new Stopwatch();
        sw.Start();
        var permutations = Bruteforce.GetAllCitiesPermutations(tspFileReader.ImMatrix.Length);
        sw.Stop();

        Console.WriteLine($" Permutations took {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        var chunkedPermutations = permutations
            .Chunk(5040)//40320 //5040
            .Select(x => x.ToList())
            .ToList();
        sw.Stop();
        Console.WriteLine($" Chunking took {sw.ElapsedMilliseconds} ms {chunkedPermutations.Count}");
        MaxMessagesCount = chunkedPermutations.Count;
        sw.Restart();

        for (int i = 0; i < chunkedPermutations.Count; i++)
        {
            Sender.SendMessage(new TspInput
            {
                Algoritm = TspAlgoritms.Bruteforce,
                Matrix = tspFileReader.ImMatrix,
                TspGeneticInput = null,
                TspBruteforceInput = new TspBruteforceInput
                {
                    Permutations = chunkedPermutations[i]
                }
            });
        }
        sw.Stop();
        Console.WriteLine($" Sending took {sw.ElapsedMilliseconds} ms");
    }
}
