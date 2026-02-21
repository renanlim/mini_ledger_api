using BtgLedger.Domain.Strategies;

namespace BtgLedger.Domain.Factories
{
    public static class TransactionStrategyFactory
    {
        // Esse método recebe a string que vem da API e devolve a classe certa!
        public static ITransactionStrategy Create(string type)
        {
            return type.ToUpper() switch
            {
                "CREDIT" => new CreditStrategy(),
                "DEBIT" => new DebitStrategy(),
                "REFUND" => new RefundStrategy(), // Nosso estorno adicionado facilmente!
                _ => throw new ArgumentException($"Invalid transaction type: {type}")
            };
        }
    }
}