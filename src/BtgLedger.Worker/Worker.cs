using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BtgLedger.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _hostname = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";    
    private readonly string[] _queues = { "transaction_completed", "sms_notifications" };

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de Mensageria Centralizado iniciado em: {time}", DateTimeOffset.Now);

        var factory = new ConnectionFactory() { HostName = _hostname };

        // Estabelece a conexão física e o canal lógico de forma assíncrona
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        foreach (var queueName in _queues)
        {
            // Garante que a fila exista antes de começar a consumir (Idempotência)
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);

            // Handler de processamento de mensagens
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogWarning("----------------------------------------");
                    _logger.LogWarning("✉️  MENSAGEM RECEBIDA DA FILA: {queue}", ea.RoutingKey);
                    _logger.LogInformation("Conteudo JSON: {message}", message);
                    _logger.LogWarning("----------------------------------------\n");

                    // CONFIRMAÇÃO POSITIVA (ACK): Notifica o RabbitMQ que a mensagem foi processada 
                    // com sucesso e pode ser removida da fila.
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem da fila {queue}", queueName);
                    
                    // CONFIRMAÇÃO NEGATIVA (NACK): Em caso de erro, a mensagem volta para a fila 
                    // (requeue: true) para ser tentada novamente mais tarde.
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            // Inicia o consumo com 'autoAck: false' para termos controle total sobre a entrega da mensagem
            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false, 
                consumer: consumer);

            _logger.LogInformation("Escutando mensagens na fila: {queue}", queueName);
        }

        // Mantém o Worker vivo enquanto o serviço não for interrompido
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}