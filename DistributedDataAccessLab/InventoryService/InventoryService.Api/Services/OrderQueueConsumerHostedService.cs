using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace InventoryService.Api.Services;

/// <summary>
/// Consumes orders-queue with manual ack (EventingBasicConsumer, BasicAck, autoAck: false).
/// </summary>
public class OrderQueueConsumerHostedService : IHostedService
{
    private readonly ILogger<OrderQueueConsumerHostedService> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public OrderQueueConsumerHostedService(
        ILogger<OrderQueueConsumerHostedService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var host = ResolveRabbitHost();
        _logger.LogInformation("RabbitMQ consumer using host {Host}:5672 (orders-queue).", host);

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        const int maxAttempts = 40;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                _connection = factory.CreateConnection();
                break;
            }
            catch (BrokerUnreachableException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ not reachable ({Host}:5672), attempt {Attempt}/{Max}. Retrying in 2s...",
                    host, attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        if (_connection == null)
            throw new InvalidOperationException(
                $"Could not connect to RabbitMQ at {host}:5672 after {maxAttempts} attempts. " +
                "In Docker, set RabbitMQ__HostName=rabbitmq (service name).");

        _channel = _connection.CreateModel();

        const string queueName = "orders-queue";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var channel = _channel;
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Order queue message received: {Message}", message);

            try
            {
                using var doc = JsonDocument.Parse(message);
                var type = doc.RootElement.TryGetProperty("eventType", out var t) ? t.GetString() : null;
                if (string.Equals(type, "OrderCreated", StringComparison.OrdinalIgnoreCase))
                    _logger.LogInformation("Inventory service acknowledged OrderCreated (async path / logging).");
                else if (string.Equals(type, "OrderCancelled", StringComparison.OrdinalIgnoreCase))
                    _logger.LogInformation("Inventory service acknowledged OrderCancelled.");
            }
            catch (JsonException)
            {
                _logger.LogWarning("Non-JSON message on orders-queue.");
            }

            channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Waiting for messages on {Queue}.", queueName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error closing RabbitMQ consumer.");
        }

        return Task.CompletedTask;
    }

    private string ResolveRabbitHost()
    {
        var h = _configuration["RabbitMQ:HostName"];
        if (!string.IsNullOrWhiteSpace(h))
            return h.Trim();

        h = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
        if (!string.IsNullOrWhiteSpace(h))
            return h.Trim();

        return "localhost";
    }
}
