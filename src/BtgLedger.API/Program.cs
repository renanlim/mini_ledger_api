using Microsoft.EntityFrameworkCore;
using BtgLedger.Infrastructure.Persistence;
using BtgLedger.Domain.Interfaces;
using BtgLedger.Infrastructure.Repositories;
using BtgLedger.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Defina a mesma chave que usamos no Controller
var key = Encoding.ASCII.GetBytes("MinhaChaveSuperSecretaDoBtgLedger2026!!");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
      // No BTG real, validaríamos Issuer e Audience para maior segurança!
    };
});

// 1. Adiciona suporte a Controllers (Padrão corporativo para APIs estruturadas)
builder.Services.AddControllers();

builder.Services.AddMemoryCache(); // NOVO: Habilita o cache em memória para o nosso 2FA!

// 2. Mantém a configuração de OpenAPI (Documentação da sua API)
builder.Services.AddOpenApi();

// 1. ADICIONE O CORS AQUI (Libera geral para ambiente de desenvolvimento)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. Configura o Banco de Dados (Entity Framework Core)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ADICIONE ESTA LINHA AQUI! (A mágica da Injeção de Dependência)
// AddScoped significa: Cria uma instância nova por cada requisição HTTP.
builder.Services.AddScoped<IAccountQueryRepository, AccountRepository>();
builder.Services.AddScoped<IAccountCommandRepository, AccountRepository>();
builder.Services.AddSingleton<IMessageBusService, RabbitMqService>();

var app = builder.Build();

// 5. Configura o pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 2. USE O CORS AQUI (Tem que ser antes do MapControllers!)
app.UseCors("AllowAll");
app.UseAuthentication(); // Primeiro autentica (quem é você?)
app.UseAuthorization();  // Depois autoriza (o que você pode fazer?)

app.UseHttpsRedirection();

// 6. Ensina a API a direcionar as requisições para os nossos Controllers
app.MapControllers();

// Aplica as migrações pendentes na base de dados automaticamente ao arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();