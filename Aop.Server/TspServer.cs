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
    private int ConsumersCount { get; set; } = 0;
    private int Cost { get; set; }
    private List<int> Path { get; set; }
    private int OptimalValue { get; set; }
    private int ReceivedMessagesCount = 0;
    private int MaxMessagesCount = 0;
    private int TimeTaken = 0;

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
        if (tspOutput.Cost >= Cost) return;

        Cost = tspOutput.Cost;
        Path = tspOutput.BestPath;
        Console.WriteLine($" [x] Received cost: {tspOutput.Cost}, Received so far: {ReceivedMessagesCount + 1}");
    }

    private void ResponseToSendersMessage(BasicDeliverEventArgs eventArgs, TspAlgoritms tspAlgoritm)
    {
        switch (tspAlgoritm)
        {
            case TspAlgoritms.Bruteforce:
                Receiver.Channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
                ReceivedMessagesCount++;
                break;
            case TspAlgoritms.Genetic:
                break;
        }
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
        StopExecution();
    }

    private void PrintResult()
    {
        Console.WriteLine($"\n [-] Task completed!");
        Console.WriteLine($" [-] Total messages received {ReceivedMessagesCount}");
        Console.WriteLine($" [-] Task completed in {TimeTaken} ms");
        Console.WriteLine($" [-] Average time for message to complete: {ConsumersCount * TimeTaken / ReceivedMessagesCount} ms");
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

    private void PrepareQueueForGenetic()
    {

    }

    private void StopExecution()
    {
        Receiver.ClearQueue();
        Receiver.Channel.Close();
        Stopwatch.Stop();
        TimeTaken = Convert.ToInt32(Stopwatch.ElapsedMilliseconds);
    }
}
