using BtgLedger.Domain.Entities;

namespace BtgLedger.Domain.Strategies
{
    public class DebitStrategy : ITransactionStrategy
    {
        public void Execute(Account account, decimal amount)
        {
            account.ProcessDebit(amount); 
        }
    }
}