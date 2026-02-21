using BtgLedger.Domain.Entities;

namespace BtgLedger.Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account> GetByIdAsync(Guid id);
        Task CreateAsync(Account account);
        Task AddTransactionAsync(Guid accountId, decimal amount, string type);
    }
}