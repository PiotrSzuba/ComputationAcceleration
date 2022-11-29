using Aop.RabbitMQ;
using Aop.RabbitMQ.TSP;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text.Json;
using RabbitMQ.Client.Events;
using System.Collections.Immutable;
using Aop.RabbitMQ.Permutations;
using Aop.RabbitMQ.Extensions;
using System.Linq;
using static Aop.RabbitMQ.TSP.Genetic;

namespace Aop.Server;

public class TspServer
{
    private Sender<TspInput> Sender { get; set; }
    private Receiver<TspOutput> Receiver { get; set; }
    private Stopwatch Stopwatch { get; set; }
    private int ConsumersCount { get; set; } = 0;
    private int Cost { get; set; }
    private List<int> Path { get; set; }
    private int OptimalValue { get; set; }
    private int ReceivedMessagesCount = 0;
    private int MaxMessagesCount = 0;
    private int NoImprove = 0;
    private int TimeTaken = 0;
    private int CitiesCount = 0;
    private Guid TaskId;
    private ImmutableArray<ImmutableArray<int>> Matrix;
    private readonly ImmutableArray<ImmutableArray<int>> EmptyMatrix = ImmutableArray<ImmutableArray<int>>.Empty;
    private List<Individual> Migrants = new();

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
        CitiesCount = tspFileReader.ImMatrix.Length;
        ConsumersCount = Sender.GetReceiversCount();
        NoImprove = 0;
        Matrix = tspFileReader.ImMatrix;

        if (!IsRunning)
        {
            Receiver.ReconnectToChannel();
        }

        Receiver.ClearQueue();

        ReceivedMessagesCount = 0;

        Console.WriteLine($" [*] Server started! Amount of hungry consumers: {Sender.GetReceiversCount()}");

        Receiver.Consumer.Received += (object? sender, BasicDeliverEventArgs eventArgs) 
            => OnReceive(sender, eventArgs, tspAlgoritm);

        Receiver.Channel.BasicConsume(queue: Receiver.QueueName,autoAck: false,consumer: Receiver.Consumer);

        PrepereQueue(tspAlgoritm, tspFileReader);
    }

    private void OnReceive(object? sender, BasicDeliverEventArgs eventArgs, TspAlgoritms tspAlgoritm)
    {
        var tspOutput = Receiver.DeserializeInput(eventArgs);
        CheckForBetterSolution(tspOutput);
        ResponseToSendersMessage(eventArgs, tspAlgoritm);

        CheckForStop(tspAlgoritm);
    }

    private void CheckForBetterSolution(TspOutput tspOutput)
    {
        if (tspOutput.Cost >= Cost)
        {
            if (tspOutput.NoImproveRuns.HasValue)
            {
                NoImprove += tspOutput.NoImproveRuns.Value;
                return;
            }
            NoImprove++;
            return;
        }

        if (tspOutput.Migrants is not null)
        {
            var size = tspOutput.Migrants.Count;
            Migrants.AddRange(tspOutput.Migrants);
            Migrants = Migrants.OrderBy(x => x.Cost).ToList();
            Migrants.RemoveRange(size, Migrants.Count - size);
        }

        NoImprove = ConsumersCount == 1 ? tspOutput.NoImproveRuns.Value : 0;
        Cost = tspOutput.Cost;
        Path = tspOutput.BestPath;
        Console.WriteLine($" [x] Received cost: {tspOutput.Cost}, Received so far: {ReceivedMessagesCount + 1}");
    }

    private void ResponseToSendersMessage(BasicDeliverEventArgs eventArgs, TspAlgoritms tspAlgoritm)
    {
        switch (tspAlgoritm)
        {
            case TspAlgoritms.Bruteforce:
                break;
            case TspAlgoritms.Genetic:
                Sender.SendMessage(new TspInput
                {
                    TaskId = TaskId,
                    Algoritm = TspAlgoritms.Genetic,
                    Matrix = EmptyMatrix,
                    TspGeneticInput = new TspGeneticInput
                    {
                        Individual = Path,
                        Migrants = Migrants,
                    }
                });
                break;
        }
        Receiver.Channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
        ReceivedMessagesCount++;
    }

    private void CheckForStop(TspAlgoritms tspAlgoritm)
    {
        switch (tspAlgoritm)
        {
            case TspAlgoritms.Bruteforce:
                BruteforceStop();
                break;
            case TspAlgoritms.Genetic:
                GeneticStop();
                break;
        }
    }

    private void BruteforceStop()
    {
        if (ReceivedMessagesCount != MaxMessagesCount) return;

        StopExecution();

        PrintResult();
    }

    private void GeneticStop()
    {
        if (NoImprove <= (ConsumersCount - 1) * CitiesCount + CitiesCount / 2) return;
        StopExecution();
        PrintResult();
    }


    private void PrintResult()
    {
        Console.WriteLine($"\n [-] Task completed!");
        Console.WriteLine($" [-] Total messages received {ReceivedMessagesCount}");
        Console.WriteLine($" [-] Task completed in {TimeTaken} ms");
        // Console.WriteLine($" [-] Average time for message to complete: {ConsumersCount * TimeTaken / ReceivedMessagesCount} ms");
        Console.WriteLine($" [-] Calculated cost: {Cost}");
        Console.WriteLine($" [-] Target value was {OptimalValue}\n");
    }

    private void PrepereQueue(TspAlgoritms tspAlgoritm, TspFileReader tspFileReader)
    {
        switch (tspAlgoritm)
        {
            case TspAlgoritms.Bruteforce:
                PrepareQueueForBruteForce(tspFileReader);
                break;
            case TspAlgoritms.Genetic:
                PrepareQueueForGenetic(tspFileReader);
                break;
        }
    }

    private void PrepareQueueForBruteForce(TspFileReader tspFileReader)
    {
        int permsPerMsg = (int)Factorial.GetFactorial(10);

        var possiblePermutations = Enumerable
            .Range(1, tspFileReader.ImMatrix.Length - 1)
            .Aggregate(1, (p, item) => p * item);

        TaskId = Guid.NewGuid();

        var messagesCount = possiblePermutations <= permsPerMsg ? 1 : possiblePermutations / permsPerMsg;

        MaxMessagesCount = messagesCount;

        for (int i = 0; i < messagesCount; i++)
        {
            Sender.SendMessage(new TspInput
            {
                TaskId = TaskId,
                Algoritm = TspAlgoritms.Bruteforce,
                Matrix = tspFileReader.ImMatrix,
                TspBruteforceInput = new TspBruteforceInput
                {
                    FirstPermutationIndex = GetFirstPermutationIndex(permsPerMsg, possiblePermutations, i),
                    LastPermutationIndex = GetLastPermutationIndex(permsPerMsg, possiblePermutations, i),
                }
            });
        }
    }

    private static int GetFirstPermutationIndex(int permsPerMsg, int possiblePermutations, int index)
        => possiblePermutations <= permsPerMsg ? 0 : permsPerMsg * index;

    private static int GetLastPermutationIndex(int permsPerMsg, int possiblePermutations, int index)
        => possiblePermutations <= permsPerMsg ? possiblePermutations - 1 : (permsPerMsg * (index + 1)) - 1;

    private void PrepareQueueForGenetic(TspFileReader tspFileReader)
    {
        var individual = new List<int>();

        for (int i = 0; i < tspFileReader.ImMatrix.Length; i++)
        {
            individual.Add(i);
        }

        TaskId = Guid.NewGuid();
        for(int i = 0; i < ConsumersCount; i++)
        {
            Sender.SendMessage(new TspInput
            {
                TaskId = TaskId,
                Algoritm = TspAlgoritms.Genetic,
                Matrix = tspFileReader.ImMatrix,
                TspGeneticInput = new TspGeneticInput
                {
                    Individual = individual.AsRandom().ToList(),
                    PopulationMultiplier = 60 / ConsumersCount,
                }
            });
        }
    }

    private void StopExecution()
    {
        Receiver.ClearQueue();
        Receiver.Channel.Close();
        Stopwatch.Stop();
        TimeTaken = Convert.ToInt32(Stopwatch.ElapsedMilliseconds);
    }
}
