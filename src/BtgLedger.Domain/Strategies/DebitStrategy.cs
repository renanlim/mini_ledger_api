using BtgLedger.Domain.Entities;

namespace BtgLedger.Domain.Strategies
{
    public class DebitStrategy : ITransactionStrategy
    {
        public void Execute(Account account, decimal amount)
        {
            // O Liskov atua aqui: A estratégia executa perfeitamente a regra de débito
            account.ProcessDebit(amount); 
        }
    }
}