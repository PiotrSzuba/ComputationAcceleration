using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aop.RabbitMQ;

public class Sender<T>
{
    private ConnectionFactory Factory { get; set; }
    private IConnection Connection { get; set; }
    private IModel Channel { get; set; }

    public Sender()
    {
        Factory = new ConnectionFactory() { HostName = "localhost" };
        Connection = Factory.CreateConnection();
        Channel = Connection.CreateModel();
        Channel.QueueDeclare(queue: typeof(T).Name,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
    }

    public void SendMessage(T input)
    {
        var message = JsonSerializer.Serialize(input);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = Channel.CreateBasicProperties();
        properties.Persistent = true;

        Channel.BasicPublish(exchange: "",
                             routingKey: typeof(T).Name,
                             basicProperties: properties,
                             body: body);
    }

    public int GetReceiversCount() => (int)Channel.ConsumerCount(typeof(T).Name);
}
