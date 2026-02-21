using BtgLedger.Domain.Entities;

namespace BtgLedger.Domain.Strategies
{
    public class RefundStrategy : ITransactionStrategy
    {
        public void Execute(Account account, decimal amount) => account.ProcessRefund(amount);
    }
}