using BtgLedger.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BtgLedger.Tests
{
    public class AccountTests
    {
        [Fact]
        public void ProcessDebit_Should_DecreaseBalance_When_FundsAreSufficient()
        {
            var account = new Account("Renan Lima", 1000m, "0001", "12345", "hash", "21999999999");
            account.ProcessDebit(200m);
            account.Balance.Should().Be(800m);
        }

        [Fact]
        public void AddCredit_Should_IncreaseBalance()
        {
            var account = new Account("Renan Lima", 1000m, "0001", "12345", "hash", "21999999999");
            account.AddCredit(500m);
            account.Balance.Should().Be(1500m);
        }

        [Fact]
        public void ProcessRefund_Should_IncreaseBalance()
        {
            var account = new Account("Renan Lima", 100m, "0001", "12345", "hash", "21999999999");
            account.ProcessRefund(50m);
            account.Balance.Should().Be(150m);
        }

        [Fact]
        public void ProcessDebit_Should_ThrowException_When_AmountExceedsBalance()
        {
            var account = new Account("Renan Lima", 50m, "0001", "12345", "hash", "21999999999");
            
            Action action = () => account.ProcessDebit(100m);
            
            action.Should().Throw<InvalidOperationException>()
                  .WithMessage("Insufficient funds.");
        }
        
        [Fact]
        public void AddCredit_Should_ThrowException_When_AmountIsZeroOrNegative()
        {
            var account = new Account("Renan Lima", 100m, "0001", "12345", "hash", "21999999999");
            
            Action action = () => account.AddCredit(-10m);
            
            action.Should().Throw<ArgumentException>()
                  .WithMessage("Amount must be greater than zero.");
        }
    }
}