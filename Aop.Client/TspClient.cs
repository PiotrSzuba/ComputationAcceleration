using Aop.RabbitMQ;
using Aop.RabbitMQ.TSP;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text.Json;

namespace Aop.Client;

public class TspClient
{
    public Sender<TspOutput> Sender { get; set; }
    public Receiver<TspInput> Receiver { get; set; }

    public TspClient()
    {
        Sender = new Sender<TspOutput>();
        Receiver = new Receiver<TspInput>();
    }
    public void Run()
    {

        Console.WriteLine(" [*] Waiting for messages.");

        Receiver.Consumer.Received += (sender, ea) =>
        {
            var tspInput = Receiver.DeserializeInput(ea);
            Console.WriteLine($" [x] Received matrix of len: {tspInput.Matrix.Length}");

            var sw = new Stopwatch();
            sw.Start();
            var result = Receiver.RunTsp(tspInput);
            sw.Stop();
            int proccesTime = Convert.ToInt32(sw.ElapsedMilliseconds);

            Console.WriteLine($" [x] Done -> Cost: {result.Cost} Time: {proccesTime} ms");

            Receiver.Channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

            Sender.SendMessage(result);
        };
        Receiver.Channel.BasicConsume(queue: "task_queue",
                             autoAck: false,
                             consumer: Receiver.Consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}
