using BtgLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BtgLedger.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Representa a tabela de contas. O EF Core usará o nome da propriedade 
        // para nomear a tabela no SQL Server como "Accounts".
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                // Define explicitamente o UUID (Guid) como Chave Primária (PK)
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.OwnerName)
                      .IsRequired()
                      .HasMaxLength(100);

                // REGRA DE OURO FINANCEIRA: 
                // O tipo 'decimal' no C# precisa ser mapeado para 'decimal(18,2)' no SQL 
                // para evitar erros de arredondamento comuns em tipos 'float' ou 'double'.
                entity.Property(e => e.Balance)
                      .HasPrecision(18, 2); 

                // Mapeamento de propriedades de identificação bancária
                entity.Property(e => e.Agency).IsRequired().HasMaxLength(4);
                entity.Property(e => e.Number).IsRequired().HasMaxLength(10);
            });
        }
    }
}