using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Aop.RabbitMQ;

public class Receiver<T>
{
    public const int PrefetchCount = 10;
    private ConnectionFactory Factory { get; set; }
    private IConnection Connection { get; set; }
    public IModel Channel { get; set; }
    public EventingBasicConsumer Consumer { get; set; }

    public string QueueName => typeof(T).Name;

    public Receiver()
    {
        Factory = new ConnectionFactory() { HostName = Config.IP };
        Connection = Factory.CreateConnection();
        Channel = Connection.CreateModel();
        Channel.QueueDeclare(queue: QueueName,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
        Channel.BasicQos(prefetchSize: 0, prefetchCount: PrefetchCount, global: false);
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
        Channel.BasicQos(prefetchSize: 0, prefetchCount: PrefetchCount, global: false);
        Consumer = new EventingBasicConsumer(Channel);
    }

    public void ClearQueue() => Channel.QueuePurge(QueueName);
}
