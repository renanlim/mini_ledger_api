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

        public AccountRepository(AppDbContext context, IMessageBusService messageBus)
        {
            _context = context;
            _messageBus = messageBus;
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await _context.Accounts.FindAsync(id);
        }

        public async Task<Account?> GetByAgencyAndNumberAsync(string agency, string number)
        {
            // Busca otimizada usando os campos de identificação bancária
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
            // GARANTIA DE ATOMICIDADE: Inicia uma transação explícita no banco de dados. 
            // Ou tudo acontece com sucesso, ou nada é alterado.
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var account = await GetByIdAsync(accountId);
                if (account == null) throw new Exception("Account not found.");

                // PADRÃO STRATEGY: Delegamos a lógica de negócio (cálculo de saldo) para a estratégia 
                // correspondente, mantendo o repositório limpo e focado em persistência.
                var strategy = TransactionStrategyFactory.Create(type);
                strategy.Execute(account, amount);

                _context.Accounts.Update(account);
                
                // Persiste a alteração do saldo antes de confirmar a transação
                await _context.SaveChangesAsync();
                
                // Finaliza a transação no banco de dados
                await transaction.CommitAsync();

                // EVENT-DRIVEN: Notifica outros serviços sobre a transação concluída.
                // Isso acontece fora do commit para não travar o banco se a mensageria demorar.
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
                // ROLLBACK: Em caso de falha ou erro de regra de negócio (ex: saldo insuficiente),
                // desfazemos qualquer alteração pendente no banco.
                await transaction.RollbackAsync();
                throw; 
            }
        }
    }
}