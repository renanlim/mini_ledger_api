namespace BtgLedger.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; set; }
        public string OwnerName { get; set; }
        public decimal Balance { get; private set; } 

        public string Agency { get; private set; }
        public string Number { get; private set; }
        public string PasswordHash { get; private set; }
        public string PhoneNumber { get; private set; }

        public Account(string ownerName, decimal initialBalance, string agency, string number, string passwordHash, string phoneNumber)
        {
            Id = Guid.NewGuid();
            OwnerName = ownerName;
            Balance = initialBalance;
            Agency = agency;
            Number = number;
            PasswordHash = passwordHash;
            PhoneNumber = phoneNumber;
        }

        protected Account() 
        { 
            OwnerName = null!;
            Agency = null!;
            Number = null!;
            PasswordHash = null!;
            PhoneNumber = null!;
        }

        // public void UpdateBalance(decimal amount, string type)
        // {
        //     if (amount <= 0)
        //     {
        //         throw new ArgumentException("Amount must be greater than zero.");
        //     }

        //     if (type.ToUpper() == "DEBIT")
        //     {
        //         if (Balance < amount)
        //         {
        //             // Regra de Negócio Crucial: Não pode ficar negativo
        //             throw new InvalidOperationException("Insufficient funds for this transaction.");
        //         }
        //         Balance -= amount;
        //     }
        //     else if (type.ToUpper() == "CREDIT")
        //     {
        //         Balance += amount;
        //     }
        //     else 
        //     {
        //         throw new ArgumentException("Invalid transaction type. Use 'DEBIT' or 'CREDIT'.");
        //     }
        // }

        public void AddCredit(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
            Balance += amount;
        }
        public void ProcessDebit(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
            if (Balance < amount) throw new InvalidOperationException("Insufficient funds for this transaction.");
            Balance -= amount;
        }
        public void ProcessRefund(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
            Balance += amount;
        }
    }
}