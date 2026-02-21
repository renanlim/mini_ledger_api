using BtgLedger.Domain.Factories;
using BtgLedger.Domain.Strategies;
using FluentAssertions;
using Xunit;

namespace BtgLedger.Tests
{
    public class TransactionStrategyFactoryTests
    {
        // O [Theory] permite testar vários cenários passando parâmetros diferentes para o mesmo teste!
        [Theory]
        [InlineData("DEBIT", typeof(DebitStrategy))]
        [InlineData("CREDIT", typeof(CreditStrategy))]
        [InlineData("REFUND", typeof(RefundStrategy))]
        [InlineData("debit", typeof(DebitStrategy))] // Testa se ignora maiúscula/minúscula
        public void Create_Should_ReturnCorrectStrategy_BasedOnType(string type, Type expectedStrategyType)
        {
            var strategy = TransactionStrategyFactory.Create(type);
            strategy.Should().BeOfType(expectedStrategyType);
        }

        [Fact]
        public void Create_Should_ThrowException_When_TypeIsInvalid()
        {
            Action action = () => TransactionStrategyFactory.Create("PIX");
            
            action.Should().Throw<ArgumentException>()
                  .WithMessage("Invalid transaction type: PIX");
        }
    }
}