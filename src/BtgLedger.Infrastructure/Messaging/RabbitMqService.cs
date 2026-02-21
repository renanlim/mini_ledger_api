using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BtgLedger.Infrastructure.Messaging
{
    public class RabbitMqService : IMessageBusService, IAsyncDisposable
    {
        // Tenta ler do Docker; se não achar, usa o localhost do seu Mac
        private readonly string _hostname = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        private IConnection? _connection;
        private IChannel? _channel;

        private async Task<IChannel> GetChannelAsync()
        {
            // 1. Se já temos um canal aberto e a funcionar, devolvemos imediatamente
            if (_channel is { IsOpen: true })
                return _channel;

            // 2. Se não temos conexão (ou se ela caiu), criamos uma nova
            if (_connection is null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory() { HostName = _hostname };
                _connection = await factory.CreateConnectionAsync();
            }

            // 3. Cria o canal de comunicação
            _channel = await _connection.CreateChannelAsync();
            return _channel;
        }

        public async Task PublishEventAsync<T>(string queue, T message)
        {
            // Pegamos no canal persistente
            var channel = await GetChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                body: body);
        }

        // Método obrigatório do IAsyncDisposable para fechar a porta quando a API desligar
        public async ValueTask DisposeAsync()
        {
            if (_channel is not null) await _channel.CloseAsync();
            if (_connection is not null) await _connection.CloseAsync();
        }
    }
}