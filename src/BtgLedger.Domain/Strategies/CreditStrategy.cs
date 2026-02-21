using BtgLedger.Domain.Entities;

namespace BtgLedger.Domain.Strategies
{
    public class CreditStrategy : ITransactionStrategy
    {
        public void Execute(Account account, decimal amount)
        {
            account.AddCredit(amount);
        }
    }
}