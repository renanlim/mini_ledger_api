using BtgLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BtgLedger.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Isso aqui vai virar uma tabela chamada "Accounts" no banco de dados
        public DbSet<Account> Accounts { get; set; }

        // Usamos esse método para configurar detalhes finos das tabelas
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.OwnerName)
                      .IsRequired()
                      .HasMaxLength(100);

                // Configuração CRÍTICA para mercado financeiro:
                // Decimal com 18 casas totais e 2 casas decimais.
                entity.Property(e => e.Balance)
                      .HasPrecision(18, 2); 
            });
        }
    }
}