using BtgLedger.Domain.Entities;

namespace BtgLedger.Domain.Strategies
{
    public interface ITransactionStrategy
    {
        void Execute(Account account, decimal amount);
    }
}