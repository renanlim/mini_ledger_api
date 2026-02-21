namespace BtgLedger.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; set; }
        public string OwnerName { get; set; }
        
        // "private set" protege o saldo. Ninguém pode fazer "account.Balance = 1000000" fora daqui.
        // Isso é encapsulamento, pilar da Orientação a Objetos.
        public decimal Balance { get; private set; } 

        // NOVOS CAMPOS BANCÁRIOS
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

        // Método para validar se é possível criar a conta (Empty Constructor para EF Core)
       // Construtor vazio usado pelo Entity Framework Core
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

        // Substitua o método UpdateBalance inteiro por estes dois métodos específicos:
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
            // O estorno devolve o dinheiro. Poderíamos ter regras extras aqui no futuro 
            // (ex: travar estornos maiores que X valor), mas por enquanto, ele soma ao saldo.
            Balance += amount;
        }
    }
}