using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BtgLedger.Infrastructure.Messaging
{
    public class RabbitMqService : IMessageBusService, IAsyncDisposable
    {
        // Flexibilidade de infraestrutura: Priorita o host do Docker/K8s, mas permite rodar local no Mac
        private readonly string _hostname = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        private IConnection? _connection;
        private IChannel? _channel;

        private async Task<IChannel> GetChannelAsync()
        {
            // Padrão Singleton/Lazy: Evita o "overhead" de abrir uma nova conexão TCP a cada mensagem enviada
            if (_channel is { IsOpen: true })
                return _channel;

            if (_connection is null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory() { HostName = _hostname };
                _connection = await factory.CreateConnectionAsync();
            }

            _channel = await _connection.CreateChannelAsync();
            return _channel;
        }

        public async Task PublishEventAsync<T>(string queue, T message)
        {
            var channel = await GetChannelAsync();

            // Idempotência: Garante que a fila exista antes de tentar publicar nela.
            // Durable=true garante que as mensagens sobrevivam a um restart do RabbitMQ.
            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true, 
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            // Publicação direta (Default Exchange): A mensagem é roteada usando o nome da fila como chave
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                body: body);
        }

        // Graceful Shutdown: Garante que as conexões TCP e canais AMQP sejam fechados 
        // corretamente quando a aplicação parar, evitando conexões "zumbis" no servidor
        public async ValueTask DisposeAsync()
        {
            if (_channel is not null) await _channel.CloseAsync();
            if (_connection is not null) await _connection.CloseAsync();
        }
    }
}