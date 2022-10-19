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

        var result = RunTsp(tspInput);

        sw.Stop();

        Console.WriteLine($" [x] Done -> Cost: {result.Cost} Time: {sw.ElapsedMilliseconds} ms");

        Receiver.Channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);

        Sender.SendMessage(result);
    }

    private static TspOutput RunTsp(TspInput tspInput)
    {
        return tspInput.Algoritm switch
        {
            TspAlgoritms.Bruteforce => Bruteforce.Run(tspInput),
            TspAlgoritms.Genetic => new Genetic(tspInput).Run(),
            _ => TspOutput.Error,
        };
    }

    private void PrepareForNewTask(TspInput tspInput)
    {
        TaskId = tspInput.TaskId;
    }
}
