namespace BtgLedger.Infrastructure.Messaging
{
    public interface IMessageBusService
    {
        Task PublishEventAsync<T>(string queue, T message); 
    }
}