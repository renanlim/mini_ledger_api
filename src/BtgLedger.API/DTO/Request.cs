namespace BtgLedger.API.DTOs
{
    public class CreateAccountRequest
    {
        public required string OwnerName { get; set; }
        public decimal InitialBalance { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Password { get; set; }
    }

    public class TransactionRequest
    {
        public decimal Amount { get; set; }
        public required string Type { get; set; }
    }
    
    public class LoginRequest
    {
        public required string Agency { get; set; }
        public required string Number { get; set; }
        public required string Password { get; set; }
    }

    public class ValidatePinRequest
    {
        public Guid AccountId { get; set; }
        public required string Pin { get; set; }
    }
}