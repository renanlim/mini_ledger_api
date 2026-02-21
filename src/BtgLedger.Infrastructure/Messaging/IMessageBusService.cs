namespace BtgLedger.Infrastructure.Messaging
{
    public interface IMessageBusService
    {
        // Alterado de void para Task e o nome para ter o sufixo Async
        Task PublishEventAsync<T>(string queue, T message); 
    }
}