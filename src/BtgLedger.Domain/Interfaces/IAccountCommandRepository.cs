using BtgLedger.Domain.Entities;

namespace BtgLedger.Domain.Interfaces
{
    public interface IAccountCommandRepository
    {
        Task CreateAsync(Account account);
        Task AddTransactionAsync(Guid accountId, decimal amount, string type);
    }
}