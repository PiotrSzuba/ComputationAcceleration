using Aop.RabbitMQ;
using Aop.RabbitMQ.TSP;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text.Json;

namespace Aop.Client;

public class TspClient
{
    private Sender<TspOutput> Sender { get; set; }
    private Receiver<TspInput> Receiver { get; set; }
    private Guid TaskId { get; set; }
    private Genetic? Genetic { get; set; } = null;

    public TspClient()
    {
        Sender = new Sender<TspOutput>();
        Receiver = new Receiver<TspInput>();
    }

    public void Run()
    {
        Console.WriteLine(" [*] Client started");

        Receiver.Consumer.Received += OnReceived;

        Receiver.Channel.BasicConsume(queue: Receiver.QueueName, autoAck: false, consumer: Receiver.Consumer);
    }

    private void OnReceived(object? sender, BasicDeliverEventArgs eventArgs)
    {
        var sw = new Stopwatch();
        sw.Start();
        var tspInput = Receiver.DeserializeInput(eventArgs);

        if (TaskId != tspInput.TaskId)
            PrepareForNewTask(tspInput);

        if (Genetic is null)
            Console.WriteLine("genetic is null");

        var result = RunTsp(tspInput);
        sw.Stop();

        Console.WriteLine($" [x] Done -> Cost: {result.Cost} Time: {sw.ElapsedMilliseconds} ms");

        RespondToMessage(eventArgs, tspInput.Algoritm);

        Sender.SendMessage(result);
    }

    private TspOutput RunTsp(TspInput tspInput)
    {
        return tspInput.Algoritm switch
        {
            TspAlgoritms.Bruteforce => Bruteforce.Run(tspInput),
            TspAlgoritms.Genetic => Genetic is null ? TspOutput.Error : Genetic.Run(tspInput),
                _ => TspOutput.Error,
        };
    }

    private void RespondToMessage(BasicDeliverEventArgs eventArgs, TspAlgoritms tspAlgoritm)
    {
        switch (tspAlgoritm)
        {
            case TspAlgoritms.Bruteforce:
                Receiver.Channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
                break;
            case TspAlgoritms.Genetic:
                Receiver.Channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
                break;
        }
    }

    private void PrepareForNewTask(TspInput tspInput)
    {
        TaskId = tspInput.TaskId;
        switch (tspInput.Algoritm)
        {
            case TspAlgoritms.Bruteforce:
                break;
            case TspAlgoritms.Genetic:
                Genetic = new Genetic(tspInput);
                break;
        }
    }
}
