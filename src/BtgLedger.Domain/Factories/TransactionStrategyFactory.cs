using BtgLedger.Domain.Strategies;

namespace BtgLedger.Domain.Factories
{
    public static class TransactionStrategyFactory
    {
        public static ITransactionStrategy Create(string type)
        {
            return type.ToUpper() switch
            {
                "CREDIT" => new CreditStrategy(),
                "DEBIT" => new DebitStrategy(),
                "REFUND" => new RefundStrategy(),
                _ => throw new ArgumentException($"Invalid transaction type: {type}")
            };
        }
    }
}