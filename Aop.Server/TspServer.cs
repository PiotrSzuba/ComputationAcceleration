using Aop.RabbitMQ;
using Aop.RabbitMQ.TSP;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text.Json;
using RabbitMQ.Client.Events;

namespace Aop.Server;

public class TspServer
{
    private Sender<TspInput> Sender { get; set; }
    private Receiver<TspOutput> Receiver { get; set; }
    private Stopwatch Stopwatch { get; set; }
    private int ReceivedMessagesCount = 0;

    public bool isRunning => Receiver.Channel.IsOpen && Receiver.Consumer.IsRunning;

    public TspServer()
    {
        Sender = new Sender<TspInput>();
        Receiver = new Receiver<TspOutput>();
        Stopwatch = new();
    }

    public void Run()
    {
        Stopwatch = new();
        Stopwatch.Start();

        var tspFileReader = new TspFileReader("gr96.tsp");
        var input = new TspInput { Matrix = tspFileReader.ImMatrix };
        var consumersCount = Sender.GetReceiversCount();

        if (!isRunning)
        {
            Receiver.ReconnectToChannel();
        }

        Receiver.ClearQueue();

        Console.WriteLine($" [*] Server started amount of hungry consumers: {Sender.GetReceiversCount()}");

        for (int i = 0; i < consumersCount; i++)
        {
            Sender.SendMessage(input);
        }

        Receiver.Consumer.Received += OnReceive;

        Receiver.Channel.BasicConsume(queue: Receiver.QueueName,autoAck: false,consumer: Receiver.Consumer);

        ReceivedMessagesCount = 0;
    }

    private void OnReceive(object? sender, BasicDeliverEventArgs eventArgs)
    {
        var tspOutput = Receiver.DeserializeInput(eventArgs);

        Console.WriteLine($" [x] Received cost: {tspOutput.Cost}, Received so far: {ReceivedMessagesCount + 1}");

        Receiver.Channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
        ReceivedMessagesCount++;

        if (ReceivedMessagesCount == 2)
        {
            Receiver.Channel.Close();
            Stopwatch.Stop();
            int proccesTime = Convert.ToInt32(Stopwatch.ElapsedMilliseconds);

            Console.WriteLine($"Task completed in {proccesTime} ms");
        }


        // Sender.SendMessage(new TspInput());
    }
}
