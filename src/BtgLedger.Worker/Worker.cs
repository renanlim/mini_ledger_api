using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BtgLedger.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _hostname = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";    
    // Lista das filas que o BTG Ledger utiliza
    private readonly string[] _queues = { "transaction_completed", "sms_notifications" };

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de Mensageria Centralizado iniciado em: {time}", DateTimeOffset.Now);

        var factory = new ConnectionFactory() { HostName = _hostname };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Configuramos cada fila dentro de um loop
        foreach (var queueName in _queues)
        {
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    // Identificamos de qual fila a mensagem veio para dar um log personalizado
                    _logger.LogWarning("----------------------------------------");
                    _logger.LogWarning("✉️  MENSAGEM RECEBIDA DA FILA: {queue}", ea.RoutingKey);
                    _logger.LogInformation("Conteúdo JSON: {message}", message);
                    _logger.LogWarning("----------------------------------------\n");

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem da fila {queue}", queueName);
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Escutando mensagens na fila: {queue}", queueName);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}