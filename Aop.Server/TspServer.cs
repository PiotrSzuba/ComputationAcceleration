using Aop.RabbitMQ;
using Aop.RabbitMQ.TSP;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text.Json;
using RabbitMQ.Client.Events;
using System.Collections.Immutable;
using Aop.RabbitMQ.Permutations;

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

        Console.WriteLine($" [*] Server started! Amount of hungry consumers: {Sender.GetReceiversCount()}");

        Receiver.Consumer.Received += OnReceive;

        Receiver.Channel.BasicConsume(queue: Receiver.QueueName,autoAck: false,consumer: Receiver.Consumer);

        ReceivedMessagesCount = 0;

        PrepereQueue(tspAlgoritm, tspFileReader);
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

            Console.WriteLine($"\n [-] Task completed!");
            Console.WriteLine($" [-] Total messages received {ReceivedMessagesCount}");
            Console.WriteLine($" [-] Task completed in {proccesTime} ms");
            Console.WriteLine($" [-] Average time for message to complete: {ConsumersCount * proccesTime / ReceivedMessagesCount} ms");
            Console.WriteLine($" [-] Calculated cost: {Cost}");
            Console.WriteLine($" [-] Target value was {OptimalValue}\n");
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
        int permsPerMsg = (int)Factorial.GetFactorial(10);

        var possiblePermutations = Enumerable
            .Range(1, tspFileReader.ImMatrix.Length - 1)
            .Aggregate(1, (p, item) => p * item);

        var taskId = Guid.NewGuid();

        var messagesCount = possiblePermutations <= permsPerMsg ? 1 : possiblePermutations / permsPerMsg;

        MaxMessagesCount = messagesCount;

        for (int i = 0; i < messagesCount; i++)
        {
            Sender.SendMessage(new TspInput
            {
                TaskId = taskId,
                Algoritm = TspAlgoritms.Bruteforce,
                Matrix = tspFileReader.ImMatrix,
                TspGeneticInput = null,
                TspBruteforceInput = new TspBruteforceInput
                {
                    FirstPermutationIndex = GetFirstPermutationIndex(permsPerMsg, possiblePermutations, i),
                    LastPermutationIndex = GetLastPermutationIndex(permsPerMsg, possiblePermutations, i),
                }
            });
        }
    }

    private int GetFirstPermutationIndex(int permsPerMsg, int possiblePermutations, int index)
        => possiblePermutations <= permsPerMsg ? 0 : permsPerMsg * index;

    private int GetLastPermutationIndex(int permsPerMsg, int possiblePermutations, int index)
        => possiblePermutations <= permsPerMsg ? possiblePermutations - 1 : (permsPerMsg * (index + 1)) - 1;
}
