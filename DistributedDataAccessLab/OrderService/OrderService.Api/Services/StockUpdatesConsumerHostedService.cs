using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace OrderService.Api.Services;

/// <summary>
/// Consumes stock-updates-queue with manual ack (EventingBasicConsumer, BasicAck, autoAck: false).
/// </summary>
public class StockUpdatesConsumerHostedService : IHostedService
{
    private readonly ILogger<StockUpdatesConsumerHostedService> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public StockUpdatesConsumerHostedService(
        ILogger<StockUpdatesConsumerHostedService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var host = ResolveRabbitHost();
        _logger.LogInformation("RabbitMQ consumer using host {Host}:5672 (stock-updates-queue).", host);

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

        const string queueName = "stock-updates-queue";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var channel = _channel;
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("Stock update event: {Message}", message);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Consuming {Queue}.", queueName);
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
