using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aop.RabbitMQ.TSP;
using System.Diagnostics;
using System.Text.Json;

namespace Aop.RabbitMQ;

public class Receiver<T>
{
    private ConnectionFactory Factory { get; set; }
    private IConnection Connection { get; set; }
    public IModel Channel { get; set; }
    public EventingBasicConsumer Consumer { get; set; }

    public string QueueName => typeof(T).Name;

    public Receiver()
    {
        Factory = new ConnectionFactory() { HostName = "localhost" };
        Connection = Factory.CreateConnection();
        Channel = Connection.CreateModel();
        Channel.QueueDeclare(queue: QueueName,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
        Channel.BasicQos(prefetchSize: 0, prefetchCount: 20, global: false);
        Consumer = new EventingBasicConsumer(Channel);
    }

    public T DeserializeInput(BasicDeliverEventArgs eventArgument)
    {
        var body = eventArgument.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var tspInput = JsonSerializer.Deserialize<T>(message);

        if (tspInput is null)
            throw new Exception("Failed to deserialize message");

        return tspInput;
    }

    public void ReconnectToChannel()
    {
        Channel = Connection.CreateModel();
        Channel.QueueDeclare(queue: QueueName,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
        Channel.BasicQos(prefetchSize: 0, prefetchCount: 20, global: false);
        Consumer = new EventingBasicConsumer(Channel);
    }

    public void ClearQueue() => Channel.QueuePurge(QueueName);
}
