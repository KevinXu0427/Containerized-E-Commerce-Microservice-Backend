using System.Text;
using RabbitMQ.Client;

namespace InventoryService.Api.Services;

/// <summary>
/// Publishes to a named queue (default exchange, routing key = queue name).
/// </summary>
public class RabbitMqPublisher
{
    private readonly string _hostname;
    private readonly string _queueName;

    public RabbitMqPublisher(string hostname, string queueName)
    {
        _hostname = hostname;
        _queueName = queueName;
    }

    public void Publish(string message)
    {
        var factory = new ConnectionFactory
        {
            HostName = _hostname,
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _queueName,
            basicProperties: null,
            body: body);
    }
}
