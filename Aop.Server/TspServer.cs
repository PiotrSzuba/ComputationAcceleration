using Aop.RabbitMQ;
using Aop.RabbitMQ.TSP;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text.Json;

namespace Aop.Server;

public class TspServer
{
    public Sender<TspInput> Sender { get; set; }
    public Receiver<TspOutput> Receiver { get; set; }

    public TspServer()
    {
        Sender = new Sender<TspInput>();
        Receiver = new Receiver<TspOutput>();
    }

    public void Run()
    {
        Console.WriteLine(" [*] Waiting for messages.");

        Receiver.Consumer.Received += (sender, ea) =>
        {
            var tspOutput = Receiver.DeserializeInput(ea);
            Console.WriteLine($" [x] Received path of len: {tspOutput.BestPath.Count}");


            Receiver.Channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

            Sender.SendMessage(new TspInput());
        };
        Receiver.Channel.BasicConsume(queue: "task_queue",
                             autoAck: false,
                             consumer: Receiver.Consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}
