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

    public TspClient()
    {
        Sender = new Sender<TspOutput>();
        Receiver = new Receiver<TspInput>();
    }

    public void Run()
    {

        Console.WriteLine(" [*] Waiting for messages.");

        Receiver.Consumer.Received += OnReceived;

        Receiver.Channel.BasicConsume(queue: Receiver.QueueName,autoAck: false,consumer: Receiver.Consumer);
    }

    private void OnReceived(object? sender, BasicDeliverEventArgs eventArgs)
    {
        var tspInput = Receiver.DeserializeInput(eventArgs);

        Console.WriteLine($" [x] Received matrix of len: {tspInput.Matrix.Length}");

        var sw = new Stopwatch();
        sw.Start();
        var result = RunTsp(tspInput);
        sw.Stop();
        int proccesTime = Convert.ToInt32(sw.ElapsedMilliseconds);

        Console.WriteLine($" [x] Done -> Cost: {result.Cost} Time: {proccesTime} ms");

        Receiver.Channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);

        Sender.SendMessage(result);
    }

    private static TspOutput RunTsp(TspInput tspInput)
    {
        return new Genetic(tspInput).Run();
    }
}
