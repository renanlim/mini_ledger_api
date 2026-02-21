using BtgLedger.Domain.Entities;

namespace BtgLedger.Domain.Interfaces
{
    public interface IAccountQueryRepository
    {
        Task<Account?> GetByIdAsync(Guid id);
        Task<Account?> GetByAgencyAndNumberAsync(string agency, string number);
    }
}