using BtgLedger.Domain.Entities;
using BtgLedger.Domain.Factories;
using BtgLedger.Domain.Interfaces;
using BtgLedger.Infrastructure.Persistence;
using BtgLedger.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BtgLedger.Infrastructure.Repositories
{
    public class AccountRepository : IAccountQueryRepository, IAccountCommandRepository
    {
        private readonly AppDbContext _context;
        private readonly IMessageBusService _messageBus;

        // Injeção de Dependência do DbContext
        public AccountRepository(AppDbContext context, IMessageBusService messageBus)
        {
            _context = context;
            _messageBus = messageBus;
        }
        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await _context.Accounts.FindAsync(id);
        }

        // Implementação da busca por agência e conta
        public async Task<Account?> GetByAgencyAndNumberAsync(string agency, string number)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(account => account.Agency == agency && account.Number == number);
        }

        public async Task CreateAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();
        }

        public async Task AddTransactionAsync(Guid accountId, decimal amount, string type)
        {
            // 1. Inicia a transação. Se algo der erro a partir daqui, NADA é salvo no banco.
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Busca a conta no banco
                var account = await GetByIdAsync(accountId);
                
                if (account == null) throw new Exception("Account not found.");

                // A MÁGICA ACONTECE AQUI: A factory pega a string e devolve a estratégia
                var strategy = TransactionStrategyFactory.Create(type);
                
                // Executa a regra sem fazer um monte de IF/ELSE
                strategy.Execute(account, amount);

                _context.Accounts.Update(account);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var evento = new 
                { 
                    AccountId = accountId, 
                    Amount = amount, 
                    Type = type, 
                    Timestamp = DateTime.UtcNow 
                };
                
                await _messageBus.PublishEventAsync("transaction_completed", evento);
            }
            catch (Exception)
            {
                // Se qualquer erro acontecer (ex: sem saldo, banco cair), desfazemos tudo (ROLLBACK)
                await transaction.RollbackAsync();
                throw; // Repassa o erro para a API tratar
            }
        }
    }
}